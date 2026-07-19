using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Inventory;
using ServiceBridge.Application.Payments;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;
using ServiceBridge.Infrastructure.Repositories;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Inventory;

public sealed class AdminInventoryCodeFulfillmentSqlServerTests :
    IClassFixture<InventoryAllocationSqlServerFixture>
{
    private const string ProtectedTestPayload =
        "protected-stage-3b1b-test-payload";
    private readonly InventoryAllocationSqlServerFixture _fixture;

    public AdminInventoryCodeFulfillmentSqlServerTests(
        InventoryAllocationSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerInventoryAllocationFact]
    public async Task SuccessfulRetry_CommitsFulfillmentAtomically()
    {
        var scenario = await SeedScenarioAsync(includeInventory: true);
        var paymentBefore = await ReadPaymentSnapshotAsync(scenario.CompletedPaymentId);

        await using (var context = _fixture.CreateContext())
        {
            var result = await CreateService(
                    context,
                    new FakeRealtimeEventPublisher())
                .RetryAsync(scenario.AdminId, scenario.OrderId);

            Assert.True(result.Success);
            Assert.Equal(
                InventoryCodeFulfillmentOutcome.Fulfilled,
                result.Data?.Outcome);
        }

        await using var verificationContext = _fixture.CreateContext();
        var paymentAfter = await ReadPaymentSnapshotAsync(
            verificationContext,
            scenario.CompletedPaymentId);
        var order = await verificationContext.Orders
            .Include(candidate => candidate.StatusHistory)
            .SingleAsync(candidate => candidate.Id == scenario.OrderId);
        var item = await verificationContext.InventoryItems
            .SingleAsync(candidate => candidate.Id == scenario.ItemId);

        Assert.Equal(paymentBefore, paymentAfter);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.Equal(scenario.OrderId, item.ReservedForOrderId);
        Assert.Equal(
            1,
            await RetryAuditCountAsync(verificationContext, scenario.OrderId));
        Assert.Equal(
            1,
            await CompletionNotificationCountAsync(
                verificationContext,
                scenario.OrderId));
        AssertFulfillmentHistory(order, expectedCount: 1);
    }

    [SqlServerInventoryAllocationFact]
    public async Task NoInventory_CommitsAcceptedRetryWithoutFulfillmentMutation()
    {
        var scenario = await SeedScenarioAsync(includeInventory: false);
        var paymentBefore = await ReadPaymentSnapshotAsync(scenario.CompletedPaymentId);

        await using (var context = _fixture.CreateContext())
        {
            var result = await CreateService(
                    context,
                    new FakeRealtimeEventPublisher())
                .RetryAsync(scenario.AdminId, scenario.OrderId);

            Assert.True(result.Success);
            Assert.Equal(
                InventoryCodeFulfillmentOutcome.NoInventory,
                result.Data?.Outcome);
        }

        await using var verificationContext = _fixture.CreateContext();
        var order = await verificationContext.Orders
            .Include(candidate => candidate.StatusHistory)
            .SingleAsync(candidate => candidate.Id == scenario.OrderId);

        Assert.Equal(
            paymentBefore,
            await ReadPaymentSnapshotAsync(
                verificationContext,
                scenario.CompletedPaymentId));
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(
            0,
            await verificationContext.AgentAssignments.CountAsync(candidate =>
                candidate.OrderId == scenario.OrderId));
        Assert.Equal(
            0,
            await verificationContext.InventoryItems.CountAsync(candidate =>
                candidate.ReservedForOrderId == scenario.OrderId));
        Assert.Equal(
            1,
            await RetryAuditCountAsync(verificationContext, scenario.OrderId));
        Assert.Equal(
            0,
            await CompletionNotificationCountAsync(
                verificationContext,
                scenario.OrderId));
        AssertFulfillmentHistory(order, expectedCount: 0);
    }

    [SqlServerInventoryAllocationFact]
    public async Task PersistenceFailure_AfterFulfillmentMutation_RollsBackEverything()
    {
        var scenario = await SeedScenarioAsync(includeInventory: true);
        var paymentBefore = await ReadPaymentSnapshotAsync(scenario.CompletedPaymentId);
        var orderLock = new OrderLockObservationInterceptor();
        var failure = new RetryAtomicityFailureInterceptor(scenario.OrderId);

        await using (var context = _fixture.CreateContext(orderLock, failure))
        {
            var service = CreateService(context, new FakeRealtimeEventPublisher());

            await Assert.ThrowsAsync<RetryAtomicityProbeException>(() =>
                service.RetryAsync(scenario.AdminId, scenario.OrderId));
        }

        Assert.True(orderLock.Observed);
        Assert.True(failure.ObservedCompletedOrder);
        Assert.True(failure.ObservedDeliveredInventory);
        Assert.True(failure.ObservedRetryAudit);
        Assert.True(failure.ObservedCompletionNotification);
        Assert.True(failure.ObservedProcessingHistory);
        Assert.True(failure.ObservedCompletedHistory);

        await using var verificationContext = _fixture.CreateContext();
        var order = await verificationContext.Orders
            .Include(candidate => candidate.StatusHistory)
            .SingleAsync(candidate => candidate.Id == scenario.OrderId);
        var item = await verificationContext.InventoryItems
            .SingleAsync(candidate => candidate.Id == scenario.ItemId);

        Assert.Equal(
            paymentBefore,
            await ReadPaymentSnapshotAsync(
                verificationContext,
                scenario.CompletedPaymentId));
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(InventoryItemStatus.Available, item.Status);
        Assert.Null(item.ReservedForOrderId);
        Assert.Null(item.ReservedAtUtc);
        Assert.Null(item.DeliveredAtUtc);
        Assert.Equal(
            0,
            await RetryAuditCountAsync(verificationContext, scenario.OrderId));
        Assert.Equal(
            0,
            await CompletionNotificationCountAsync(
                verificationContext,
                scenario.OrderId));
        AssertFulfillmentHistory(order, expectedCount: 0);
    }

    [SqlServerInventoryAllocationFact]
    public async Task ConcurrentRetries_ForOneOrderAndItem_FulfillExactlyOnce()
    {
        var scenario = await SeedScenarioAsync(includeInventory: true);
        var firstSave = new BlockingSaveChangesInterceptor();
        var secondOrderLock = new OrderLockObservationInterceptor();

        await using var firstContext = _fixture.CreateContext(firstSave);
        await using var secondContext = _fixture.CreateContext(secondOrderLock);
        var firstTask = CreateService(
                firstContext,
                new FakeRealtimeEventPublisher())
            .RetryAsync(scenario.AdminId, scenario.OrderId);

        await firstSave.WaitUntilEnteredAsync();
        var secondTask = CreateService(
                secondContext,
                new FakeRealtimeEventPublisher())
            .RetryAsync(scenario.AdminId, scenario.OrderId);

        try
        {
            await secondOrderLock.WaitUntilObservedAsync();
        }
        finally
        {
            firstSave.Release();
        }

        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.All(results, result => Assert.True(result.Success));
        Assert.Contains(
            results,
            result => result.Data?.Outcome ==
                      InventoryCodeFulfillmentOutcome.Fulfilled);
        Assert.Contains(
            results,
            result => result.Data?.Outcome ==
                      InventoryCodeFulfillmentOutcome.AlreadyFulfilled);

        await using var verificationContext = _fixture.CreateContext();
        var order = await verificationContext.Orders
            .Include(candidate => candidate.StatusHistory)
            .SingleAsync(candidate => candidate.Id == scenario.OrderId);
        var item = await verificationContext.InventoryItems
            .SingleAsync(candidate => candidate.Id == scenario.ItemId);

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.Equal(scenario.OrderId, item.ReservedForOrderId);
        Assert.Equal(
            1,
            await verificationContext.InventoryItems.CountAsync(candidate =>
                candidate.ReservedForOrderId == scenario.OrderId));
        Assert.Equal(
            1,
            await CompletionNotificationCountAsync(
                verificationContext,
                scenario.OrderId));
        AssertFulfillmentHistory(order, expectedCount: 1);
    }

    [SqlServerInventoryAllocationFact]
    public async Task ConcurrentRetries_WithoutStock_BothCommitNoInventorySafely()
    {
        var scenario = await SeedScenarioAsync(includeInventory: false);
        var firstSave = new BlockingSaveChangesInterceptor();
        var secondOrderLock = new OrderLockObservationInterceptor();

        await using var firstContext = _fixture.CreateContext(firstSave);
        await using var secondContext = _fixture.CreateContext(secondOrderLock);
        var firstTask = CreateService(
                firstContext,
                new FakeRealtimeEventPublisher())
            .RetryAsync(scenario.AdminId, scenario.OrderId);

        await firstSave.WaitUntilEnteredAsync();
        var secondTask = CreateService(
                secondContext,
                new FakeRealtimeEventPublisher())
            .RetryAsync(scenario.AdminId, scenario.OrderId);

        try
        {
            await secondOrderLock.WaitUntilObservedAsync();
        }
        finally
        {
            firstSave.Release();
        }

        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.All(results, result =>
        {
            Assert.True(result.Success);
            Assert.Equal(
                InventoryCodeFulfillmentOutcome.NoInventory,
                result.Data?.Outcome);
        });

        await using var verificationContext = _fixture.CreateContext();
        var order = await verificationContext.Orders
            .Include(candidate => candidate.StatusHistory)
            .SingleAsync(candidate => candidate.Id == scenario.OrderId);

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(
            0,
            await verificationContext.InventoryItems.CountAsync(candidate =>
                candidate.ReservedForOrderId == scenario.OrderId));
        Assert.Equal(
            0,
            await CompletionNotificationCountAsync(
                verificationContext,
                scenario.OrderId));
        Assert.Equal(
            2,
            await RetryAuditCountAsync(verificationContext, scenario.OrderId));
        AssertFulfillmentHistory(order, expectedCount: 0);
    }

    [SqlServerInventoryAllocationFact]
    public async Task DuplicatePaymentConfirmationAndAdminRetry_CompleteSafely()
    {
        var scenario = await SeedScenarioAsync(includeInventory: true);
        var paymentBefore = await ReadPaymentSnapshotAsync(
            scenario.CompletedPaymentId);
        var synchronization = new PaymentAdminConcurrencySynchronization();

        await using var paymentContext = _fixture.CreateContext(
            new PaymentOrderLockInterceptor(synchronization));
        await using var adminContext = _fixture.CreateContext(
            new AdminOrderLockInterceptor(synchronization));
        var paymentService = CreatePaymentService(
            paymentContext,
            new FakeRealtimeEventPublisher());
        var adminService = CreateService(
            adminContext,
            new FakeRealtimeEventPublisher());

        var paymentTask = paymentService.ConfirmAsync(
            new ConfirmPaymentRequest
            {
                PaymentId = scenario.CompletedPaymentId,
                Status = PaymentStatus.Completed,
                GatewayReference = "stage-3b1b-duplicate-completed"
            },
            "{}");

        await synchronization.WaitUntilPaymentOrderLockedAsync();

        var adminTask = adminService.RetryAsync(
            scenario.AdminId,
            scenario.OrderId);

        await synchronization.WaitUntilAdminOrderRequestedAsync();

        await Task.WhenAll(paymentTask, adminTask)
            .WaitAsync(TimeSpan.FromSeconds(30));

        var paymentResult = await paymentTask;
        var adminResult = await adminTask;
        Assert.True(paymentResult.Success);
        Assert.True(adminResult.Success);
        Assert.Equal(
            InventoryCodeFulfillmentOutcome.AlreadyFulfilled,
            adminResult.Data?.Outcome);

        await using var verificationContext = _fixture.CreateContext();
        var paymentAfter = await ReadPaymentSnapshotAsync(
            verificationContext,
            scenario.CompletedPaymentId);
        var order = await verificationContext.Orders
            .Include(candidate => candidate.StatusHistory)
            .SingleAsync(candidate => candidate.Id == scenario.OrderId);

        Assert.Equal(paymentBefore, paymentAfter);
        Assert.Equal(PaymentStatus.Completed, paymentAfter.Status);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(
            1,
            await verificationContext.InventoryItems.CountAsync(candidate =>
                candidate.ReservedForOrderId == scenario.OrderId &&
                candidate.Status == InventoryItemStatus.Delivered));
        Assert.Equal(
            1,
            await verificationContext.InventoryItems.CountAsync(candidate =>
                candidate.Id == scenario.ItemId &&
                candidate.ReservedForOrderId == scenario.OrderId));
        AssertFulfillmentHistory(order, expectedCount: 1);
        Assert.Equal(
            1,
            await CompletionNotificationCountAsync(
                verificationContext,
                scenario.OrderId));
        var completionNotification = await verificationContext.Notifications
            .SingleAsync(candidate =>
                candidate.RelatedEntityId == scenario.OrderId &&
                candidate.Type == NotificationType.OrderCompleted);
        Assert.DoesNotContain(
            ProtectedTestPayload,
            completionNotification.Message);
        Assert.NotNull(scenario.CodeFingerprint);
        Assert.DoesNotContain(
            scenario.CodeFingerprint,
            completionNotification.Message);
        Assert.Equal(
            1,
            await RetryAuditCountAsync(verificationContext, scenario.OrderId));
        Assert.Equal(
            0,
            await verificationContext.AgentAssignments.CountAsync(candidate =>
                candidate.OrderId == scenario.OrderId));
    }

    [SqlServerInventoryAllocationFact]
    public async Task LatestPayment_DeterministicallyControlsRetryEligibility()
    {
        var now = DateTime.UtcNow;
        var olderCompleted = new PaymentSeed(
            Guid.NewGuid(),
            now.AddMinutes(-2),
            Completed: true);
        var newerPending = new PaymentSeed(
            Guid.NewGuid(),
            now.AddMinutes(-1),
            Completed: false);
        var createdAtScenario = await SeedScenarioAsync(
            includeInventory: true,
            payments: [olderCompleted, newerPending]);

        await AssertLatestPaymentConflictPersistsNothingAsync(createdAtScenario);

        var sharedCreatedAt = now;
        var lowerCompleted = new PaymentSeed(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            sharedCreatedAt,
            Completed: true);
        var higherPending = new PaymentSeed(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            sharedCreatedAt,
            Completed: false);
        var idScenario = await SeedScenarioAsync(
            includeInventory: true,
            payments: [lowerCompleted, higherPending]);

        await AssertLatestPaymentConflictPersistsNothingAsync(idScenario);
    }

    [SqlServerInventoryAllocationFact]
    public async Task RealtimeFailure_AfterCommit_DoesNotAlterPersistedFulfillment()
    {
        var scenario = await SeedScenarioAsync(includeInventory: true);
        var paymentBefore = await ReadPaymentSnapshotAsync(scenario.CompletedPaymentId);

        await using (var context = _fixture.CreateContext())
        {
            var result = await CreateService(
                    context,
                    new ThrowingRealtimeEventPublisher())
                .RetryAsync(scenario.AdminId, scenario.OrderId);

            Assert.True(result.Success);
            Assert.Equal(
                InventoryCodeFulfillmentOutcome.Fulfilled,
                result.Data?.Outcome);
        }

        await using var verificationContext = _fixture.CreateContext();
        var order = await verificationContext.Orders
            .Include(candidate => candidate.StatusHistory)
            .SingleAsync(candidate => candidate.Id == scenario.OrderId);
        var item = await verificationContext.InventoryItems
            .SingleAsync(candidate => candidate.Id == scenario.ItemId);

        Assert.Equal(
            paymentBefore,
            await ReadPaymentSnapshotAsync(
                verificationContext,
                scenario.CompletedPaymentId));
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.Equal(scenario.OrderId, item.ReservedForOrderId);
        Assert.Equal(
            1,
            await RetryAuditCountAsync(verificationContext, scenario.OrderId));
        Assert.Equal(
            1,
            await CompletionNotificationCountAsync(
                verificationContext,
                scenario.OrderId));
        AssertFulfillmentHistory(order, expectedCount: 1);
    }

    private static AdminInventoryCodeFulfillmentService CreateService(
        ServiceBridgeDbContext context,
        IRealtimeEventPublisher realtimeEventPublisher)
    {
        var notifications = new NotificationService(
            new NotificationRepository(context),
            realtimeEventPublisher);
        var fulfillment = new FulfillInventoryCodeOrderCommand(
            new InventoryCodeFulfillmentRepository(context),
            notifications);

        return new AdminInventoryCodeFulfillmentService(
            new AdminOrderCommandRepository(context),
            fulfillment,
            new InventoryCodeFulfillmentTransactionManager(context),
            new AuditLogWriter(context, new HttpContextAccessor()),
            notifications,
            realtimeEventPublisher,
            NullLogger<AdminInventoryCodeFulfillmentService>.Instance);
    }

    private static PaymentService CreatePaymentService(
        ServiceBridgeDbContext context,
        IRealtimeEventPublisher realtimeEventPublisher)
    {
        var notifications = new NotificationService(
            new NotificationRepository(context),
            realtimeEventPublisher);
        var fulfillment = new FulfillInventoryCodeOrderCommand(
            new InventoryCodeFulfillmentRepository(context),
            notifications);

        return new PaymentService(
            new PaymentRepository(context),
            new FakeMockPaymentGateway(),
            new FakePaymentGatewayFactory([new FakePaymentGateway()]),
            notifications,
            realtimeEventPublisher,
            fulfillment,
            new PaymentConfirmationTransactionManager(context),
            NullLogger<PaymentService>.Instance);
    }

    private async Task<RetryScenario> SeedScenarioAsync(
        bool includeInventory,
        IReadOnlyCollection<PaymentSeed>? payments = null)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var admin = new User
        {
            FullName = "Stage 3B-1B inventory admin",
            Email = $"stage-3b1b-admin-{suffix}@example.test",
            PasswordHash = "test-only-hash",
            Role = UserRole.Admin,
            CreatedAt = now.AddMinutes(-10)
        };
        var customer = new User
        {
            FullName = "Stage 3B-1B customer",
            Email = $"stage-3b1b-customer-{suffix}@example.test",
            PasswordHash = "test-only-hash",
            Role = UserRole.User,
            CreatedAt = now.AddMinutes(-10)
        };
        var category = new ServiceCategory
        {
            Name = $"Stage 3B-1B {suffix}",
            Slug = $"stage-3b1b-{suffix}",
            IsActive = true,
            CreatedAt = now.AddMinutes(-10)
        };
        var provider = new Provider
        {
            Name = $"Stage 3B-1B provider {suffix}",
            Slug = $"stage-3b1b-provider-{suffix}",
            IsActive = true,
            CreatedAt = now.AddMinutes(-10)
        };
        var service = new DigitalService
        {
            CategoryId = category.Id,
            Category = category,
            ProviderId = provider.Id,
            Provider = provider,
            Name = $"Stage 3B-1B service {suffix}",
            Slug = $"stage-3b1b-service-{suffix}",
            CurrencyCode = "USD",
            BasePrice = 1m,
            IsActive = true,
            CreatedAt = now.AddMinutes(-10)
        };
        var variant = new ServiceVariant
        {
            ServiceId = service.Id,
            Service = service,
            Name = $"Stage 3B-1B variant {suffix}",
            Slug = $"stage-3b1b-variant-{suffix}",
            CurrencyCode = "USD",
            RegionCode = "GLOBAL",
            FulfillmentMode = FulfillmentMode.InventoryCode,
            BasePrice = 1m,
            IsActive = true,
            CreatedAt = now.AddMinutes(-10)
        };
        var order = OrderTestFactory.Create(FulfillmentMode.InventoryCode);
        order.OrderNumber = $"S3B1B-{suffix[..8]}";
        order.UserId = customer.Id;
        order.User = customer;
        order.ServiceId = service.Id;
        order.Service = service;
        order.ServiceVariantId = variant.Id;
        order.ServiceVariant = variant;
        order.OrderType = OrderType.Purchase;
        order.FulfillmentMode = OrderFulfillmentMode.UseExistingAccount;
        order.Status = OrderStatus.Created;
        order.ForeignPrice = 1m;
        order.CurrencyCode = "USD";
        order.ExchangeRateSnapshot = 1m;
        order.MarginPercentSnapshot = 0m;
        order.BaseAmountIrr = 1m;
        order.ProfitAmountIrr = 0m;
        order.FinalAmountIrr = 1m;
        order.CreatedAt = now.AddMinutes(-10);
        order.MoveToWaitingForPayment(customer.Id, now.AddMinutes(-9));

        var paymentSeeds = payments ??
        [
            new PaymentSeed(
                Guid.NewGuid(),
                now.AddMinutes(-8),
                Completed: true)
        ];
        var paymentEntities = paymentSeeds
            .Select(seed => CreatePayment(order, seed))
            .ToList();
        var completedPayment = paymentEntities.First(candidate =>
            candidate.Status == PaymentStatus.Completed);
        order.MarkPaid(completedPayment.Id, "Test payment completed.");

        InventoryItem? item = null;
        if (includeInventory)
        {
            var codeFingerprint = string.Concat(
                Guid.NewGuid().ToString("N"),
                Guid.NewGuid().ToString("N"));
            item = InventoryItem.Create(
                variant.Id,
                FulfillmentMode.InventoryCode,
                ProtectedTestPayload,
                codeFingerprint,
                admin.Id,
                now.AddMinutes(-7));
        }

        await using var context = _fixture.CreateContext();
        context.AddRange(admin, customer, category, provider, service, variant, order);
        context.Payments.AddRange(paymentEntities);
        if (item is not null)
        {
            context.InventoryItems.Add(item);
        }

        await context.SaveChangesAsync();

        return new RetryScenario(
            admin.Id,
            order.Id,
            completedPayment.Id,
            item?.Id,
            item?.CodeFingerprint);
    }

    private static Payment CreatePayment(Order order, PaymentSeed seed)
    {
        var payment = new Payment
        {
            Id = seed.Id,
            OrderId = order.Id,
            Order = order,
            AmountIRR = order.FinalAmountIrr,
            AmountUSD = order.ForeignPrice,
            ExchangeRateSnapshot = order.ExchangeRateSnapshot,
            CreatedAt = seed.CreatedAt
        };
        payment.RecordInitiated();
        if (seed.Completed)
        {
            payment.Complete(
                "stage-3b1b-completed",
                null,
                seed.CreatedAt.AddSeconds(1));
        }

        order.PaymentRecords.Add(payment);
        return payment;
    }

    private async Task AssertLatestPaymentConflictPersistsNothingAsync(
        RetryScenario scenario)
    {
        var paymentsBefore = await ReadOrderPaymentSnapshotsAsync(scenario.OrderId);

        await using (var context = _fixture.CreateContext())
        {
            var result = await CreateService(
                    context,
                    new FakeRealtimeEventPublisher())
                .RetryAsync(scenario.AdminId, scenario.OrderId);

            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
        }

        await using var verificationContext = _fixture.CreateContext();
        var order = await verificationContext.Orders
            .Include(candidate => candidate.StatusHistory)
            .SingleAsync(candidate => candidate.Id == scenario.OrderId);

        Assert.Equal(
            paymentsBefore,
            await ReadOrderPaymentSnapshotsAsync(
                verificationContext,
                scenario.OrderId));
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(
            0,
            await RetryAuditCountAsync(verificationContext, scenario.OrderId));
        Assert.Equal(
            0,
            await CompletionNotificationCountAsync(
                verificationContext,
                scenario.OrderId));
        Assert.Equal(
            0,
            await verificationContext.InventoryItems.CountAsync(candidate =>
                candidate.ReservedForOrderId == scenario.OrderId));
        AssertFulfillmentHistory(order, expectedCount: 0);
    }

    private async Task<PaymentSnapshot> ReadPaymentSnapshotAsync(Guid paymentId)
    {
        await using var context = _fixture.CreateContext();
        return await ReadPaymentSnapshotAsync(context, paymentId);
    }

    private static async Task<PaymentSnapshot> ReadPaymentSnapshotAsync(
        ServiceBridgeDbContext context,
        Guid paymentId)
    {
        return await context.Payments
            .AsNoTracking()
            .Where(candidate => candidate.Id == paymentId)
            .Select(candidate => new PaymentSnapshot(
                candidate.Id,
                candidate.Status,
                candidate.GatewayReference,
                candidate.CallbackPayload,
                candidate.ConfirmedAt,
                candidate.FailedAt,
                candidate.RefundedAt,
                candidate.UpdatedAt,
                candidate.Events.Count))
            .SingleAsync();
    }

    private async Task<IReadOnlyList<PaymentSnapshot>>
        ReadOrderPaymentSnapshotsAsync(Guid orderId)
    {
        await using var context = _fixture.CreateContext();
        return await ReadOrderPaymentSnapshotsAsync(context, orderId);
    }

    private static async Task<IReadOnlyList<PaymentSnapshot>>
        ReadOrderPaymentSnapshotsAsync(
            ServiceBridgeDbContext context,
            Guid orderId)
    {
        return await context.Payments
            .AsNoTracking()
            .Where(candidate => candidate.OrderId == orderId)
            .OrderBy(candidate => candidate.Id)
            .Select(candidate => new PaymentSnapshot(
                candidate.Id,
                candidate.Status,
                candidate.GatewayReference,
                candidate.CallbackPayload,
                candidate.ConfirmedAt,
                candidate.FailedAt,
                candidate.RefundedAt,
                candidate.UpdatedAt,
                candidate.Events.Count))
            .ToListAsync();
    }

    private static Task<int> RetryAuditCountAsync(
        ServiceBridgeDbContext context,
        Guid orderId)
    {
        return context.AuditLogs.CountAsync(candidate =>
            candidate.EntityId == orderId &&
            candidate.ActionType == AuditActionType.InventoryFulfillmentRetried);
    }

    private static Task<int> CompletionNotificationCountAsync(
        ServiceBridgeDbContext context,
        Guid orderId)
    {
        return context.Notifications.CountAsync(candidate =>
            candidate.RelatedEntityId == orderId &&
            candidate.Type == NotificationType.OrderCompleted);
    }

    private static void AssertFulfillmentHistory(Order order, int expectedCount)
    {
        Assert.Equal(
            expectedCount,
            order.StatusHistory.Count(candidate =>
                candidate.NewStatus == OrderStatus.Processing));
        Assert.Equal(
            expectedCount,
            order.StatusHistory.Count(candidate =>
                candidate.NewStatus == OrderStatus.Completed));
    }

    private sealed class RetryAtomicityFailureInterceptor : SaveChangesInterceptor
    {
        private readonly Guid _orderId;

        public RetryAtomicityFailureInterceptor(Guid orderId)
        {
            _orderId = orderId;
        }

        public bool ObservedCompletedOrder { get; private set; }
        public bool ObservedDeliveredInventory { get; private set; }
        public bool ObservedRetryAudit { get; private set; }
        public bool ObservedCompletionNotification { get; private set; }
        public bool ObservedProcessingHistory { get; private set; }
        public bool ObservedCompletedHistory { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = (ServiceBridgeDbContext)eventData.Context!;
            ObservedCompletedOrder = context.ChangeTracker
                .Entries<Order>()
                .Any(entry =>
                    entry.Entity.Id == _orderId &&
                    entry.Entity.Status == OrderStatus.Completed);
            ObservedDeliveredInventory = context.ChangeTracker
                .Entries<InventoryItem>()
                .Any(entry =>
                    entry.Entity.ReservedForOrderId == _orderId &&
                    entry.Entity.Status == InventoryItemStatus.Delivered);
            ObservedRetryAudit = context.ChangeTracker
                .Entries<AuditLog>()
                .Any(entry =>
                    entry.State == EntityState.Added &&
                    entry.Entity.EntityId == _orderId &&
                    entry.Entity.ActionType ==
                        AuditActionType.InventoryFulfillmentRetried);
            ObservedCompletionNotification = context.ChangeTracker
                .Entries<Notification>()
                .Any(entry =>
                    entry.State == EntityState.Added &&
                    entry.Entity.RelatedEntityId == _orderId &&
                    entry.Entity.Type == NotificationType.OrderCompleted);
            ObservedProcessingHistory = ObservedHistory(
                context,
                OrderStatus.Processing);
            ObservedCompletedHistory = ObservedHistory(
                context,
                OrderStatus.Completed);

            throw new RetryAtomicityProbeException();
        }

        private bool ObservedHistory(
            ServiceBridgeDbContext context,
            OrderStatus status)
        {
            return context.ChangeTracker
                .Entries<OrderStatusHistory>()
                .Any(entry =>
                    entry.State == EntityState.Added &&
                    entry.Entity.OrderId == _orderId &&
                    entry.Entity.NewStatus == status);
        }
    }

    private sealed class BlockingSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _entered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task WaitUntilEnteredAsync()
        {
            await _entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }

        public void Release()
        {
            _release.TrySetResult();
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            _entered.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
            return result;
        }
    }

    private sealed class OrderLockObservationInterceptor : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _observed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool Observed { get; private set; }

        public async Task WaitUntilObservedAsync()
        {
            await _observed.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }

        public override ValueTask<InterceptionResult<DbDataReader>>
            ReaderExecutingAsync(
                DbCommand command,
                CommandEventData eventData,
                InterceptionResult<DbDataReader> result,
                CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains(
                    "FROM [Orders] WITH (UPDLOCK, ROWLOCK)",
                    StringComparison.OrdinalIgnoreCase))
            {
                Observed = true;
                _observed.TrySetResult();
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed class PaymentAdminConcurrencySynchronization
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
        private readonly TaskCompletionSource _paymentOrderLocked =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _adminOrderRequested =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void PaymentOrderLocked()
        {
            _paymentOrderLocked.TrySetResult();
        }

        public void AdminOrderRequested()
        {
            _adminOrderRequested.TrySetResult();
        }

        public Task WaitUntilPaymentOrderLockedAsync() =>
            _paymentOrderLocked.Task.WaitAsync(Timeout);

        public Task WaitUntilAdminOrderRequestedAsync() =>
            _adminOrderRequested.Task.WaitAsync(Timeout);
    }

    private sealed class PaymentOrderLockInterceptor : DbCommandInterceptor
    {
        private readonly PaymentAdminConcurrencySynchronization _synchronization;

        public PaymentOrderLockInterceptor(
            PaymentAdminConcurrencySynchronization synchronization)
        {
            _synchronization = synchronization;
        }

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (IsOrderLock(command))
            {
                _synchronization.PaymentOrderLocked();
                await _synchronization.WaitUntilAdminOrderRequestedAsync();
            }

            return result;
        }
    }

    private sealed class AdminOrderLockInterceptor : DbCommandInterceptor
    {
        private readonly PaymentAdminConcurrencySynchronization _synchronization;

        public AdminOrderLockInterceptor(
            PaymentAdminConcurrencySynchronization synchronization)
        {
            _synchronization = synchronization;
        }

        public override ValueTask<InterceptionResult<DbDataReader>>
            ReaderExecutingAsync(
                DbCommand command,
                CommandEventData eventData,
                InterceptionResult<DbDataReader> result,
                CancellationToken cancellationToken = default)
        {
            if (IsOrderLock(command))
            {
                _synchronization.AdminOrderRequested();
            }

            return ValueTask.FromResult(result);
        }
    }

    private static bool IsOrderLock(DbCommand command)
    {
        return command.CommandText.Contains(
            "FROM [Orders] WITH (UPDLOCK, ROWLOCK)",
            StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ThrowingRealtimeEventPublisher : IRealtimeEventPublisher
    {
        public Task PublishNotificationReceivedAsync(
            Guid userId,
            RealtimeEventDto notification,
            CancellationToken cancellationToken = default) => Failed();

        public Task PublishOrderUpdatedAsync(
            OrderUpdatedRealtimeEvent orderUpdated,
            CancellationToken cancellationToken = default) => Failed();

        public Task PublishOrderAssignedAsync(
            OrderAssignedRealtimeEvent orderAssigned,
            CancellationToken cancellationToken = default) => Failed();

        public Task PublishCustomerActionChangedAsync(
            CustomerActionChangedRealtimeEvent customerActionChanged,
            CancellationToken cancellationToken = default) => Failed();

        public Task PublishTicketUpdatedAsync(
            TicketUpdatedRealtimeEvent ticketUpdated,
            CancellationToken cancellationToken = default) => Failed();

        private static Task Failed()
        {
            return Task.FromException(
                new InvalidOperationException("Test realtime failure."));
        }
    }

    private sealed class RetryAtomicityProbeException : Exception
    {
    }

    private sealed record PaymentSeed(
        Guid Id,
        DateTime CreatedAt,
        bool Completed);

    private sealed record RetryScenario(
        Guid AdminId,
        Guid OrderId,
        Guid CompletedPaymentId,
        Guid? ItemId,
        string? CodeFingerprint);

    private sealed record PaymentSnapshot(
        Guid Id,
        PaymentStatus Status,
        string? GatewayReference,
        string? CallbackPayload,
        DateTime? ConfirmedAt,
        DateTime? FailedAt,
        DateTime? RefundedAt,
        DateTime? UpdatedAt,
        int EventCount);
}
