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
using ServiceBridge.Tests.Inventory;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Payments;

public sealed class InventoryCodeFulfillmentSqlServerTests :
    IClassFixture<InventoryAllocationSqlServerFixture>
{
    private const string ProtectedTestPayload =
        "protected-stage-3a3-test-payload";
    private readonly InventoryAllocationSqlServerFixture _fixture;

    public InventoryCodeFulfillmentSqlServerTests(
        InventoryAllocationSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerInventoryAllocationFact]
    public async Task AtomicRollback_AfterInventoryMutation_PersistsNothing()
    {
        var scenario = await SeedScenarioAsync();
        var interceptor = new AtomicityFailureSaveChangesInterceptor();
        var realtime = new FakeRealtimeEventPublisher();

        await using (var context = _fixture.CreateContext(interceptor))
        {
            var service = CreateService(context, realtime);

            await Assert.ThrowsAsync<AtomicityProbeException>(() =>
                service.ConfirmAsync(CompletedRequest(scenario.PaymentId), "{}"));
        }

        Assert.True(interceptor.ObservedCompletedPayment);
        Assert.True(interceptor.ObservedCompletedOrder);
        Assert.True(interceptor.ObservedDeliveredInventory);
        Assert.True(interceptor.ObservedCompletionNotification);
        Assert.Empty(realtime.Notifications);
        Assert.Empty(realtime.OrderUpdates);

        await using var verificationContext = _fixture.CreateContext();
        var payment = await verificationContext.Payments
            .Include(candidate => candidate.Events)
            .SingleAsync(candidate => candidate.Id == scenario.PaymentId);
        var order = await verificationContext.Orders
            .Include(candidate => candidate.StatusHistory)
            .SingleAsync(candidate => candidate.Id == scenario.OrderId);
        var item = await verificationContext.InventoryItems
            .SingleAsync(candidate => candidate.Id == scenario.ItemId);
        var notificationCount = await verificationContext.Notifications
            .CountAsync(candidate => candidate.RelatedEntityId == scenario.OrderId);

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.DoesNotContain(
            payment.Events,
            paymentEvent => paymentEvent.EventType == PaymentEventType.Completed);
        Assert.Equal(OrderStatus.WaitingForPayment, order.Status);
        Assert.DoesNotContain(
            order.StatusHistory,
            history => history.NewStatus is
                OrderStatus.Processing or OrderStatus.Completed);
        Assert.Equal(InventoryItemStatus.Available, item.Status);
        Assert.Null(item.ReservedForOrderId);
        Assert.Null(item.ReservedAtUtc);
        Assert.Null(item.DeliveredAtUtc);
        Assert.Equal(0, notificationCount);
    }

    [SqlServerInventoryAllocationFact]
    public async Task SuccessfulConfirmation_PersistsFulfillmentAtomically()
    {
        var scenario = await SeedScenarioAsync();
        var realtime = new FakeRealtimeEventPublisher();

        await using (var context = _fixture.CreateContext())
        {
            var service = CreateService(context, realtime);
            var result = await service.ConfirmAsync(
                CompletedRequest(scenario.PaymentId),
                "{}");

            Assert.True(result.Success);
        }

        await AssertSuccessfulFulfillmentAsync(scenario);
        Assert.Equal(2, realtime.Notifications.Count);
        Assert.Equal(
            OrderStatus.Completed.ToString(),
            Assert.Single(realtime.OrderUpdates).Status);
        Assert.All(realtime.Notifications, notification =>
        {
            Assert.DoesNotContain(ProtectedTestPayload, notification.Message);
            Assert.DoesNotContain(scenario.CodeFingerprint, notification.Message);
        });
    }

    [SqlServerInventoryAllocationFact]
    public async Task ConcurrentConfirmations_AllocateAndNotifyExactlyOnce()
    {
        var scenario = await SeedScenarioAsync();
        var blockingRealtime = new BlockingOrderRealtimePublisher();
        var secondRealtime = new FakeRealtimeEventPublisher();

        await using var firstContext = _fixture.CreateContext();
        var firstService = CreateService(firstContext, blockingRealtime);
        var firstTask = firstService.ConfirmAsync(
            CompletedRequest(scenario.PaymentId),
            "{}");

        await blockingRealtime.WaitUntilOrderPublishAsync();

        try
        {
            await using var secondContext = _fixture.CreateContext();
            var secondService = CreateService(secondContext, secondRealtime);
            var secondResult = await secondService.ConfirmAsync(
                CompletedRequest(scenario.PaymentId),
                "{}");

            Assert.True(secondResult.Success);
            Assert.Empty(secondRealtime.Notifications);
            Assert.Empty(secondRealtime.OrderUpdates);
        }
        finally
        {
            blockingRealtime.Release();
        }

        var firstResult = await firstTask;
        Assert.True(firstResult.Success);

        await AssertSuccessfulFulfillmentAsync(scenario);

        await using var verificationContext = _fixture.CreateContext();
        Assert.Equal(
            1,
            await verificationContext.PaymentEvents.CountAsync(candidate =>
                candidate.PaymentId == scenario.PaymentId &&
                candidate.EventType == PaymentEventType.Completed));
        Assert.Equal(
            1,
            await verificationContext.OrderStatusHistory.CountAsync(candidate =>
                candidate.OrderId == scenario.OrderId &&
                candidate.NewStatus == OrderStatus.Processing));
        Assert.Equal(
            1,
            await verificationContext.OrderStatusHistory.CountAsync(candidate =>
                candidate.OrderId == scenario.OrderId &&
                candidate.NewStatus == OrderStatus.Completed));
    }

    [SqlServerInventoryAllocationFact]
    public async Task DuplicateCallbackAfterCommit_DoesNotDuplicateFulfillmentState()
    {
        var scenario = await SeedScenarioAsync();

        await using (var firstContext = _fixture.CreateContext())
        {
            var firstService = CreateService(
                firstContext,
                new FakeRealtimeEventPublisher());
            var firstResult = await firstService.ConfirmAsync(
                CompletedRequest(scenario.PaymentId),
                "{}");
            Assert.True(firstResult.Success);
        }

        DateTime? deliveredAtUtc;
        int paymentEventCount;
        int historyCount;
        int completionNotificationCount;

        await using (var snapshotContext = _fixture.CreateContext())
        {
            deliveredAtUtc = await snapshotContext.InventoryItems
                .Where(candidate => candidate.Id == scenario.ItemId)
                .Select(candidate => candidate.DeliveredAtUtc)
                .SingleAsync();
            paymentEventCount = await snapshotContext.PaymentEvents
                .CountAsync(candidate => candidate.PaymentId == scenario.PaymentId);
            historyCount = await snapshotContext.OrderStatusHistory
                .CountAsync(candidate => candidate.OrderId == scenario.OrderId);
            completionNotificationCount = await snapshotContext.Notifications
                .CountAsync(candidate =>
                    candidate.RelatedEntityId == scenario.OrderId &&
                    candidate.Type == NotificationType.OrderCompleted);
        }

        await using (var duplicateContext = _fixture.CreateContext())
        {
            var duplicateService = CreateService(
                duplicateContext,
                new FakeRealtimeEventPublisher());
            var duplicateResult = await duplicateService.ConfirmAsync(
                CompletedRequest(scenario.PaymentId),
                "{}");
            Assert.True(duplicateResult.Success);
        }

        await using var verificationContext = _fixture.CreateContext();
        var item = await verificationContext.InventoryItems
            .SingleAsync(candidate => candidate.Id == scenario.ItemId);

        Assert.Equal(deliveredAtUtc, item.DeliveredAtUtc);
        Assert.Equal(scenario.OrderId, item.ReservedForOrderId);
        Assert.Equal(
            paymentEventCount,
            await verificationContext.PaymentEvents.CountAsync(candidate =>
                candidate.PaymentId == scenario.PaymentId));
        Assert.Equal(
            historyCount,
            await verificationContext.OrderStatusHistory.CountAsync(candidate =>
                candidate.OrderId == scenario.OrderId));
        Assert.Equal(
            completionNotificationCount,
            await verificationContext.Notifications.CountAsync(candidate =>
                candidate.RelatedEntityId == scenario.OrderId &&
                candidate.Type == NotificationType.OrderCompleted));
        Assert.Equal(
            1,
            await verificationContext.InventoryItems.CountAsync(candidate =>
                candidate.ReservedForOrderId == scenario.OrderId));
    }

    [SqlServerInventoryAllocationFact]
    public async Task RealtimeFailureAfterCommit_DoesNotRollbackFulfillment()
    {
        var scenario = await SeedScenarioAsync();

        await using (var context = _fixture.CreateContext())
        {
            var service = CreateService(
                context,
                new ThrowingRealtimeEventPublisher());
            var result = await service.ConfirmAsync(
                CompletedRequest(scenario.PaymentId),
                "{}");

            Assert.True(result.Success);
        }

        await AssertSuccessfulFulfillmentAsync(scenario);
    }

    private PaymentService CreateService(
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
            new FakeFulfillmentCoordinator(),
            NullLogger<PaymentService>.Instance);
    }

    private async Task<FulfillmentScenario> SeedScenarioAsync()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var admin = new User
        {
            FullName = "Stage 3A-3 inventory admin",
            Email = $"stage-3a3-admin-{suffix}@example.test",
            PasswordHash = "test-only-hash",
            Role = UserRole.Admin,
            CreatedAt = now.AddMinutes(-5)
        };
        var customer = new User
        {
            FullName = "Stage 3A-3 customer",
            Email = $"stage-3a3-customer-{suffix}@example.test",
            PasswordHash = "test-only-hash",
            Role = UserRole.User,
            CreatedAt = now.AddMinutes(-5)
        };
        var category = new ServiceCategory
        {
            Name = $"Stage 3A-3 {suffix}",
            Slug = $"stage-3a3-{suffix}",
            IsActive = true,
            CreatedAt = now.AddMinutes(-5)
        };
        var provider = new Provider
        {
            Name = $"Stage 3A-3 provider {suffix}",
            Slug = $"stage-3a3-provider-{suffix}",
            IsActive = true,
            CreatedAt = now.AddMinutes(-5)
        };
        var service = new DigitalService
        {
            CategoryId = category.Id,
            Category = category,
            ProviderId = provider.Id,
            Provider = provider,
            Name = $"Stage 3A-3 service {suffix}",
            Slug = $"stage-3a3-service-{suffix}",
            CurrencyCode = "USD",
            BasePrice = 1m,
            IsActive = true,
            CreatedAt = now.AddMinutes(-5)
        };
        var variant = new ServiceVariant
        {
            ServiceId = service.Id,
            Service = service,
            Name = $"Stage 3A-3 variant {suffix}",
            Slug = $"stage-3a3-variant-{suffix}",
            CurrencyCode = "USD",
            RegionCode = "GLOBAL",
            FulfillmentMode = FulfillmentMode.InventoryCode,
            BasePrice = 1m,
            IsActive = true,
            CreatedAt = now.AddMinutes(-5)
        };
        var order = OrderTestFactory.Create(FulfillmentMode.InventoryCode);
        order.OrderNumber = $"S3A3-{suffix[..8]}";
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
        order.CreatedAt = now.AddMinutes(-5);
        order.MoveToWaitingForPayment(customer.Id, now.AddMinutes(-4));

        var payment = new Payment
        {
            OrderId = order.Id,
            Order = order,
            AmountIRR = order.FinalAmountIrr,
            AmountUSD = order.ForeignPrice,
            ExchangeRateSnapshot = order.ExchangeRateSnapshot,
            CreatedAt = now.AddMinutes(-3)
        };
        payment.RecordInitiated();

        var codeFingerprint = string.Concat(
            Guid.NewGuid().ToString("N"),
            Guid.NewGuid().ToString("N"));
        var item = InventoryItem.Create(
            variant.Id,
            FulfillmentMode.InventoryCode,
            ProtectedTestPayload,
            codeFingerprint,
            admin.Id,
            now.AddMinutes(-2));

        await using var context = _fixture.CreateContext();
        context.AddRange(
            admin,
            customer,
            category,
            provider,
            service,
            variant,
            order,
            payment,
            item);
        await context.SaveChangesAsync();

        return new FulfillmentScenario(
            order.Id,
            payment.Id,
            item.Id,
            codeFingerprint);
    }

    private async Task AssertSuccessfulFulfillmentAsync(
        FulfillmentScenario scenario)
    {
        await using var context = _fixture.CreateContext();
        var payment = await context.Payments
            .Include(candidate => candidate.Events)
            .SingleAsync(candidate => candidate.Id == scenario.PaymentId);
        var order = await context.Orders
            .Include(candidate => candidate.StatusHistory)
            .SingleAsync(candidate => candidate.Id == scenario.OrderId);
        var item = await context.InventoryItems
            .SingleAsync(candidate => candidate.Id == scenario.ItemId);
        var completionNotifications = await context.Notifications
            .Where(candidate =>
                candidate.RelatedEntityId == scenario.OrderId &&
                candidate.Type == NotificationType.OrderCompleted)
            .ToListAsync();

        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal(
            1,
            payment.Events.Count(candidate =>
                candidate.EventType == PaymentEventType.Completed));
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(
            1,
            order.StatusHistory.Count(candidate =>
                candidate.NewStatus == OrderStatus.Processing));
        Assert.Equal(
            1,
            order.StatusHistory.Count(candidate =>
                candidate.NewStatus == OrderStatus.Completed));
        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.Equal(scenario.OrderId, item.ReservedForOrderId);
        Assert.Single(completionNotifications);
        Assert.DoesNotContain(
            ProtectedTestPayload,
            completionNotifications[0].Message);
        Assert.DoesNotContain(
            scenario.CodeFingerprint,
            completionNotifications[0].Message);
    }

    private static ConfirmPaymentRequest CompletedRequest(Guid paymentId)
    {
        return new ConfirmPaymentRequest
        {
            PaymentId = paymentId,
            Status = PaymentStatus.Completed,
            GatewayReference = "stage-3a3-confirmed"
        };
    }

    private sealed class AtomicityFailureSaveChangesInterceptor :
        SaveChangesInterceptor
    {
        public bool ObservedCompletedPayment { get; private set; }
        public bool ObservedCompletedOrder { get; private set; }
        public bool ObservedDeliveredInventory { get; private set; }
        public bool ObservedCompletionNotification { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var context = (ServiceBridgeDbContext)eventData.Context!;
            ObservedCompletedPayment = context.ChangeTracker
                .Entries<Payment>()
                .Any(entry => entry.Entity.Status == PaymentStatus.Completed);
            ObservedCompletedOrder = context.ChangeTracker
                .Entries<Order>()
                .Any(entry => entry.Entity.Status == OrderStatus.Completed);
            ObservedDeliveredInventory = context.ChangeTracker
                .Entries<InventoryItem>()
                .Any(entry =>
                    entry.Entity.Status == InventoryItemStatus.Delivered &&
                    entry.Entity.ReservedForOrderId.HasValue);
            ObservedCompletionNotification = context.ChangeTracker
                .Entries<Notification>()
                .Any(entry =>
                    entry.State == EntityState.Added &&
                    entry.Entity.Type == NotificationType.OrderCompleted);

            throw new AtomicityProbeException();
        }
    }

    private sealed class AtomicityProbeException : Exception
    {
    }

    private sealed class ThrowingRealtimeEventPublisher :
        IRealtimeEventPublisher
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

    private sealed class BlockingOrderRealtimePublisher :
        IRealtimeEventPublisher
    {
        private readonly TaskCompletionSource _orderPublishEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task WaitUntilOrderPublishAsync()
        {
            await _orderPublishEntered.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }

        public void Release()
        {
            _release.TrySetResult();
        }

        public Task PublishNotificationReceivedAsync(
            Guid userId,
            RealtimeEventDto notification,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public async Task PublishOrderUpdatedAsync(
            OrderUpdatedRealtimeEvent orderUpdated,
            CancellationToken cancellationToken = default)
        {
            _orderPublishEntered.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
        }

        public Task PublishOrderAssignedAsync(
            OrderAssignedRealtimeEvent orderAssigned,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task PublishCustomerActionChangedAsync(
            CustomerActionChangedRealtimeEvent customerActionChanged,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task PublishTicketUpdatedAsync(
            TicketUpdatedRealtimeEvent ticketUpdated,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed record FulfillmentScenario(
        Guid OrderId,
        Guid PaymentId,
        Guid ItemId,
        string CodeFingerprint);
}
