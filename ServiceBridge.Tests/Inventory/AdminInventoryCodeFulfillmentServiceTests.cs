using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Inventory;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Infrastructure.Data;
using ServiceBridge.Infrastructure.Repositories;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Inventory;

public sealed class AdminInventoryCodeFulfillmentServiceTests
{
    private static readonly DateTime UtcNow =
        new(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task LockOrderForInventoryFulfillmentAsync_WithoutTransaction_IsRejected()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=UnusedAdminInventoryRetryGuard")
            .Options;
        using var context = new ServiceBridgeDbContext(options);
        var repository = new AdminOrderCommandRepository(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.LockOrderForInventoryFulfillmentAsync(Guid.NewGuid()));

        Assert.Equal(
            "Inventory code fulfillment requires an active database transaction.",
            exception.Message);
    }

    [Fact]
    public async Task RetryAsync_WithAvailableInventory_CommitsThenPublishes()
    {
        var operations = new List<string>();
        var order = CreatePaidInventoryOrder();
        var item = CreateInventoryItem(order.ServiceVariantId!.Value);
        var setup = CreateSetup(order, operations, nextAvailable: item);

        var result = await setup.Service.RetryAsync(Guid.NewGuid(), order.Id);

        Assert.True(result.Success);
        Assert.Equal(InventoryCodeFulfillmentOutcome.Fulfilled, result.Data?.Outcome);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.Equal(order.Id, item.ReservedForOrderId);
        Assert.Equal(1, setup.Repository.SaveCount);
        Assert.Equal(1, setup.TransactionManager.CommitCount);
        Assert.Equal(0, setup.TransactionManager.RollbackCount);
        Assert.Equal(
            new[]
            {
                "transaction.begin",
                "order.lock",
                "notification.completed.queued",
                "audit.queue",
                "order.save",
                "transaction.commit",
                "notification.publish",
                "realtime.order.publish"
            },
            operations);
        var audit = Assert.Single(setup.AuditLogWriter.QueuedEntries);
        Assert.Equal(AuditActionType.InventoryFulfillmentRetried, audit.ActionType);
        Assert.DoesNotContain("protected", audit.Description, StringComparison.OrdinalIgnoreCase);
        var realtime = Assert.Single(setup.RealtimePublisher.OrderUpdates);
        Assert.True(realtime.NotifyAdmins);
    }

    [Fact]
    public async Task RetryAsync_WithoutInventory_CommitsAuditWithoutOrderMutationOrRealtime()
    {
        var operations = new List<string>();
        var order = CreatePaidInventoryOrder();
        var updatedAt = order.UpdatedAt;
        var historyCount = order.StatusHistory.Count;
        var setup = CreateSetup(order, operations);

        var result = await setup.Service.RetryAsync(Guid.NewGuid(), order.Id);

        Assert.True(result.Success);
        Assert.Equal(InventoryCodeFulfillmentOutcome.NoInventory, result.Data?.Outcome);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(updatedAt, order.UpdatedAt);
        Assert.Equal(historyCount, order.StatusHistory.Count);
        Assert.Equal(1, setup.Repository.SaveCount);
        Assert.Equal(1, setup.TransactionManager.CommitCount);
        Assert.Empty(setup.Notifications.QueuedTypes);
        Assert.Empty(setup.RealtimePublisher.OrderUpdates);
        Assert.Equal(
            new[]
            {
                "transaction.begin",
                "order.lock",
                "audit.queue",
                "order.save",
                "transaction.commit"
            },
            operations);
    }

    [Fact]
    public async Task RetryAsync_WithReservedAllocation_ResumesWithoutDuplicateNotification()
    {
        var operations = new List<string>();
        var order = CreatePaidInventoryOrder();
        var item = CreateInventoryItem(order.ServiceVariantId!.Value);
        item.ReserveFor(order.Id, UtcNow);
        var setup = CreateSetup(order, operations, existingAllocation: item);

        var result = await setup.Service.RetryAsync(Guid.NewGuid(), order.Id);

        Assert.True(result.Success);
        Assert.Equal(
            InventoryCodeFulfillmentOutcome.ResumedExistingAllocation,
            result.Data?.Outcome);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.DoesNotContain(
            nameof(FakeNotificationService.QueueOrderCompletedAsync),
            setup.Notifications.QueuedTypes);
        Assert.DoesNotContain("notification.publish", operations);
        Assert.Contains("realtime.order.publish", operations);
    }

    [Fact]
    public async Task RetryAsync_WhenAlreadyFulfilled_CommitsAuditAsIdempotentNoOp()
    {
        var operations = new List<string>();
        var order = CreatePaidInventoryOrder();
        order.Status = OrderStatus.Completed;
        var item = CreateInventoryItem(order.ServiceVariantId!.Value);
        item.ReserveFor(order.Id, UtcNow);
        item.MarkDelivered(order.Id, UtcNow);
        var deliveredAt = item.DeliveredAtUtc;
        var setup = CreateSetup(order, operations, existingAllocation: item);

        var result = await setup.Service.RetryAsync(Guid.NewGuid(), order.Id);

        Assert.True(result.Success);
        Assert.Equal(
            InventoryCodeFulfillmentOutcome.AlreadyFulfilled,
            result.Data?.Outcome);
        Assert.Equal(deliveredAt, item.DeliveredAtUtc);
        Assert.Equal(1, setup.Repository.SaveCount);
        Assert.Equal(1, setup.TransactionManager.CommitCount);
        Assert.Empty(setup.Notifications.QueuedTypes);
        Assert.Empty(setup.RealtimePublisher.OrderUpdates);
        Assert.Single(setup.AuditLogWriter.QueuedEntries);
    }

    [Fact]
    public async Task RetryAsync_WithInconsistentAllocation_ReturnsConflictWithoutPersistence()
    {
        var order = CreatePaidInventoryOrder();
        order.Status = OrderStatus.Completed;
        var item = CreateInventoryItem(order.ServiceVariantId!.Value);
        item.ReserveFor(order.Id, UtcNow);
        var setup = CreateSetup(order, [], existingAllocation: item);

        var result = await setup.Service.RetryAsync(Guid.NewGuid(), order.Id);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(0, setup.Repository.SaveCount);
        Assert.Equal(0, setup.TransactionManager.CommitCount);
        Assert.Equal(1, setup.TransactionManager.RollbackCount);
        Assert.Empty(setup.AuditLogWriter.QueuedEntries);
        Assert.Equal(InventoryItemStatus.Reserved, item.Status);
    }

    [Fact]
    public async Task RetryAsync_ForManualAgentOrder_ReturnsConflictWithoutPersistence()
    {
        var operations = new List<string>();
        var order = CreatePaidInventoryOrder();
        order.ServiceFulfillmentModeSnapshot = FulfillmentMode.ManualAgent;
        var setup = CreateSetup(order, operations);

        var result = await setup.Service.RetryAsync(Guid.NewGuid(), order.Id);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
        Assert.Equal(0, setup.Repository.SaveCount);
        Assert.Equal(0, setup.TransactionManager.CommitCount);
        Assert.Equal(1, setup.TransactionManager.RollbackCount);
        Assert.Empty(setup.AuditLogWriter.QueuedEntries);
        Assert.Empty(setup.Notifications.QueuedTypes);
        Assert.Empty(setup.RealtimePublisher.OrderUpdates);
    }

    [Fact]
    public async Task RetryAsync_WhenLatestPaymentIsNotCompleted_ReturnsConflict()
    {
        var order = CreatePaidInventoryOrder();
        var pendingPayment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            CreatedAt = UtcNow.AddMinutes(1)
        };
        order.PaymentRecords.Add(pendingPayment);
        var setup = CreateSetup(order, []);

        var result = await setup.Service.RetryAsync(Guid.NewGuid(), order.Id);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(0, setup.InventoryRepository.ExistingAllocationCalls);
        Assert.Equal(0, setup.InventoryRepository.LockNextAvailableCalls);
        Assert.Equal(0, setup.Repository.SaveCount);
        Assert.Equal(1, setup.TransactionManager.RollbackCount);
    }

    [Fact]
    public async Task RetryAsync_WhenSaveFails_DoesNotCommitOrPublish()
    {
        var operations = new List<string>();
        var order = CreatePaidInventoryOrder();
        var setup = CreateSetup(
            order,
            operations,
            nextAvailable: CreateInventoryItem(order.ServiceVariantId!.Value));
        setup.Repository.SaveSucceeds = false;

        var result = await setup.Service.RetryAsync(Guid.NewGuid(), order.Id);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(0, setup.TransactionManager.CommitCount);
        Assert.Equal(1, setup.TransactionManager.RollbackCount);
        Assert.DoesNotContain("notification.publish", operations);
        Assert.DoesNotContain("realtime.order.publish", operations);
    }

    [Fact]
    public async Task RetryAsync_WhenPostCommitRealtimeFails_RemainsSuccessful()
    {
        var order = CreatePaidInventoryOrder();
        var setup = CreateSetup(
            order,
            [],
            nextAvailable: CreateInventoryItem(order.ServiceVariantId!.Value));
        setup.Notifications.ThrowOnPublishQueuedRealtime = true;
        setup.RealtimePublisher.ThrowOnOrderUpdate = true;

        var result = await setup.Service.RetryAsync(Guid.NewGuid(), order.Id);

        Assert.True(result.Success);
        Assert.Equal(1, setup.TransactionManager.CommitCount);
        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    private static ServiceSetup CreateSetup(
        Order order,
        IList<string> operations,
        InventoryItem? nextAvailable = null,
        InventoryItem? existingAllocation = null)
    {
        var repository = new FakeAdminOrderCommandRepository
        {
            Order = order,
            Operations = operations
        };
        var inventoryRepository = new FakeInventoryCodeFulfillmentRepository
        {
            NextAvailable = nextAvailable,
            ExistingAllocation = existingAllocation
        };
        var notifications = new FakeNotificationService { Operations = operations };
        var realtime = new FakeRealtimeEventPublisher { Operations = operations };
        var audit = new FakeAuditLogWriter { Operations = operations };
        var transactionManager =
            new FakeInventoryCodeFulfillmentTransactionManager(operations);
        var service = new AdminInventoryCodeFulfillmentService(
            repository,
            new FulfillInventoryCodeOrderCommand(
                inventoryRepository,
                notifications),
            transactionManager,
            audit,
            notifications,
            realtime,
            NullLogger<AdminInventoryCodeFulfillmentService>.Instance);

        return new ServiceSetup(
            service,
            repository,
            inventoryRepository,
            transactionManager,
            notifications,
            realtime,
            audit);
    }

    private static Order CreatePaidInventoryOrder()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-RETRY",
            UserId = Guid.NewGuid(),
            ServiceVariantId = Guid.NewGuid(),
            ServiceFulfillmentModeSnapshot = FulfillmentMode.InventoryCode,
            Status = OrderStatus.Paid
        };
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            CreatedAt = UtcNow
        };
        payment.Complete("gateway-reference", null, UtcNow);
        order.PaymentRecords.Add(payment);
        return order;
    }

    private static InventoryItem CreateInventoryItem(Guid serviceVariantId)
    {
        return InventoryItem.Create(
            serviceVariantId,
            FulfillmentMode.InventoryCode,
            "protected-value",
            "fingerprint-value",
            Guid.NewGuid(),
            UtcNow.AddMinutes(-1));
    }

    private sealed class FakeInventoryCodeFulfillmentRepository :
        IInventoryCodeFulfillmentRepository
    {
        public InventoryItem? ExistingAllocation { get; set; }
        public InventoryItem? NextAvailable { get; set; }
        public int ExistingAllocationCalls { get; private set; }
        public int LockNextAvailableCalls { get; private set; }

        public Task<InventoryItem?> GetExistingAllocationAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            ExistingAllocationCalls++;
            return Task.FromResult(ExistingAllocation);
        }

        public Task<InventoryItem?> LockNextAvailableAsync(
            Guid serviceVariantId,
            CancellationToken cancellationToken = default)
        {
            LockNextAvailableCalls++;
            return Task.FromResult(NextAvailable);
        }
    }

    private sealed record ServiceSetup(
        AdminInventoryCodeFulfillmentService Service,
        FakeAdminOrderCommandRepository Repository,
        FakeInventoryCodeFulfillmentRepository InventoryRepository,
        FakeInventoryCodeFulfillmentTransactionManager TransactionManager,
        FakeNotificationService Notifications,
        FakeRealtimeEventPublisher RealtimePublisher,
        FakeAuditLogWriter AuditLogWriter);
}
