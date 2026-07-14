using ServiceBridge.Application.Inventory;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Inventory;

public sealed class FulfillInventoryCodeOrderCommandTests
{
    private static readonly DateTime UtcNow =
        new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task NonInventoryOrder_ReturnsNotInventoryOrder()
    {
        var order = CreatePaidOrder(FulfillmentMode.ManualAgent);
        var setup = CreateSetup();

        var result = await setup.Command.ExecuteAsync(
            order,
            Guid.NewGuid(),
            UtcNow);

        AssertOutcome(result, InventoryCodeFulfillmentOutcome.NotInventoryOrder);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(0, setup.Repository.ExistingAllocationCalls);
        Assert.Empty(setup.Notifications.QueuedOrderCompletions);
    }

    [Fact]
    public async Task PaidOrderWithoutInventory_ReturnsNoInventoryAndRemainsPaid()
    {
        var order = CreatePaidOrder();
        var setup = CreateSetup();

        var result = await setup.Command.ExecuteAsync(
            order,
            Guid.NewGuid(),
            UtcNow);

        AssertOutcome(result, InventoryCodeFulfillmentOutcome.NoInventory);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(1, setup.Repository.LockCalls);
        Assert.Equal(order.ServiceVariantId, setup.Repository.LockedServiceVariantId);
        Assert.Empty(setup.Notifications.QueuedOrderCompletions);
    }

    [Fact]
    public async Task PaidOrderWithAvailableItem_FulfillsAndQueuesOneSafeNotification()
    {
        var order = CreatePaidOrder();
        const string protectedCode = "ciphertext-sensitive-payload";
        const string fingerprint = "fingerprint-sensitive-payload";
        var item = CreateAvailableItem(
            order.ServiceVariantId!.Value,
            protectedCode,
            fingerprint);
        var setup = CreateSetup(nextAvailable: item);
        var systemActorId = Guid.NewGuid();

        var result = await setup.Command.ExecuteAsync(
            order,
            systemActorId,
            UtcNow);

        AssertOutcome(result, InventoryCodeFulfillmentOutcome.Fulfilled);
        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.Equal(order.Id, item.ReservedForOrderId);
        Assert.Equal(UtcNow, item.ReservedAtUtc);
        Assert.Equal(UtcNow, item.DeliveredAtUtc);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(
            new[] { OrderStatus.Processing, OrderStatus.Completed },
            order.StatusHistory
                .Where(history => history.NewStatus is
                    OrderStatus.Processing or OrderStatus.Completed)
                .Select(history => history.NewStatus)
                .ToArray());
        Assert.All(order.StatusHistory.Where(history => history.NewStatus is
            OrderStatus.Processing or OrderStatus.Completed), history =>
        {
            Assert.Equal(OrderLogActorType.System, history.ActorType);
            Assert.Equal(systemActorId, history.ActorId);
        });

        var notification = Assert.Single(setup.Notifications.QueuedOrderCompletions);
        Assert.Equal(order.UserId, notification.UserId);
        Assert.Equal(order.Id, notification.OrderId);
        Assert.Equal(order.OrderNumber, notification.OrderNumber);
        Assert.DoesNotContain(protectedCode, notification.OrderNumber);
        Assert.DoesNotContain(fingerprint, notification.OrderNumber);
        Assert.DoesNotContain(nameof(InventoryItem.ProtectedCode), notification.OrderNumber);
        Assert.DoesNotContain(nameof(InventoryItem.CodeFingerprint), notification.OrderNumber);
        Assert.DoesNotContain(
            nameof(INotificationService.PublishQueuedRealtimeAsync),
            setup.Notifications.QueuedTypes);
    }

    [Fact]
    public async Task ExistingReservedAllocationWithPaidOrder_ResumesWithoutNewAllocation()
    {
        var order = CreatePaidOrder();
        var item = CreateReservedItem(order);
        var setup = CreateSetup(existingAllocation: item);

        var result = await setup.Command.ExecuteAsync(
            order,
            Guid.NewGuid(),
            UtcNow);

        AssertOutcome(
            result,
            InventoryCodeFulfillmentOutcome.ResumedExistingAllocation);
        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.Equal(order.Id, item.ReservedForOrderId);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(0, setup.Repository.LockCalls);
        Assert.Empty(setup.Notifications.QueuedOrderCompletions);
    }

    [Fact]
    public async Task ExistingReservedAllocationWithProcessingOrder_CompletesSameItem()
    {
        var order = CreatePaidOrder();
        order.StartInventoryFulfillment(Guid.NewGuid());
        var item = CreateReservedItem(order);
        var setup = CreateSetup(existingAllocation: item);

        var result = await setup.Command.ExecuteAsync(
            order,
            Guid.NewGuid(),
            UtcNow);

        AssertOutcome(
            result,
            InventoryCodeFulfillmentOutcome.ResumedExistingAllocation);
        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.Equal(order.Id, item.ReservedForOrderId);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(0, setup.Repository.LockCalls);
    }

    [Fact]
    public async Task ExistingDeliveredAllocationWithProcessingOrder_CompletesWithoutChangingItem()
    {
        var order = CreatePaidOrder();
        order.StartInventoryFulfillment(Guid.NewGuid());
        var item = CreateDeliveredItem(order);
        var deliveredAt = item.DeliveredAtUtc;
        var updatedAt = item.UpdatedAt;
        var setup = CreateSetup(existingAllocation: item);

        var result = await setup.Command.ExecuteAsync(
            order,
            Guid.NewGuid(),
            UtcNow);

        AssertOutcome(
            result,
            InventoryCodeFulfillmentOutcome.ResumedExistingAllocation);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(InventoryItemStatus.Delivered, item.Status);
        Assert.Equal(deliveredAt, item.DeliveredAtUtc);
        Assert.Equal(updatedAt, item.UpdatedAt);
        Assert.Equal(0, setup.Repository.LockCalls);
        Assert.Empty(setup.Notifications.QueuedOrderCompletions);
    }

    [Fact]
    public async Task ExistingDeliveredAllocationWithCompletedOrder_IsAlreadyFulfilledWithoutMutation()
    {
        var order = CreateCompletedOrder();
        var item = CreateDeliveredItem(order);
        var historyCount = order.StatusHistory.Count;
        var deliveredAt = item.DeliveredAtUtc;
        var setup = CreateSetup(existingAllocation: item);

        var result = await setup.Command.ExecuteAsync(
            order,
            Guid.NewGuid(),
            UtcNow);

        AssertOutcome(result, InventoryCodeFulfillmentOutcome.AlreadyFulfilled);
        Assert.Equal(historyCount, order.StatusHistory.Count);
        Assert.Equal(deliveredAt, item.DeliveredAtUtc);
        Assert.Equal(0, setup.Repository.LockCalls);
        Assert.Empty(setup.Notifications.QueuedOrderCompletions);
    }

    [Fact]
    public async Task CompletedOrderWithReservedAllocation_IsInconsistentWithoutClaimingAnotherItem()
    {
        var order = CreateCompletedOrder();
        var item = CreateReservedItem(order);
        var setup = CreateSetup(existingAllocation: item);

        var result = await setup.Command.ExecuteAsync(
            order,
            Guid.NewGuid(),
            UtcNow);

        AssertOutcome(result, InventoryCodeFulfillmentOutcome.InconsistentState);
        Assert.Equal(InventoryItemStatus.Reserved, item.Status);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(0, setup.Repository.LockCalls);
        Assert.Empty(setup.Notifications.QueuedOrderCompletions);
    }

    [Fact]
    public async Task UnsupportedStateWithoutAllocation_ReturnsInvalidOrderState()
    {
        var order = CreateOrder(OrderStatus.Created);
        var setup = CreateSetup();

        var result = await setup.Command.ExecuteAsync(
            order,
            Guid.NewGuid(),
            UtcNow);

        AssertOutcome(result, InventoryCodeFulfillmentOutcome.InvalidOrderState);
        Assert.Equal(OrderStatus.Created, order.Status);
        Assert.Equal(0, setup.Repository.LockCalls);
        Assert.Empty(setup.Notifications.QueuedOrderCompletions);
    }

    [Fact]
    public void Dependencies_AreLimitedToAllocationRepositoryAndNotificationQueue()
    {
        var constructor = Assert.Single(
            typeof(FulfillInventoryCodeOrderCommand).GetConstructors());

        Assert.Equal(
            new[]
            {
                typeof(IInventoryCodeFulfillmentRepository),
                typeof(INotificationService)
            },
            constructor.GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray());
    }

    private static void AssertOutcome(
        ServiceResult<InventoryCodeFulfillmentOutcome> result,
        InventoryCodeFulfillmentOutcome expected)
    {
        Assert.True(result.Success);
        Assert.Equal(expected, result.Data);
    }

    private static CommandSetup CreateSetup(
        InventoryItem? existingAllocation = null,
        InventoryItem? nextAvailable = null)
    {
        var repository = new FakeInventoryCodeFulfillmentRepository
        {
            ExistingAllocation = existingAllocation,
            NextAvailable = nextAvailable
        };
        var notifications = new FakeNotificationService();

        return new CommandSetup(
            new FulfillInventoryCodeOrderCommand(repository, notifications),
            repository,
            notifications);
    }

    private static Order CreatePaidOrder(
        FulfillmentMode serviceFulfillmentMode = FulfillmentMode.InventoryCode)
    {
        var order = CreateOrder(OrderStatus.Created, serviceFulfillmentMode);
        order.MoveToWaitingForPayment(Guid.NewGuid(), UtcNow.AddMinutes(-3));
        order.MarkPaid(Guid.NewGuid(), "Payment confirmed.");
        return order;
    }

    private static Order CreateCompletedOrder()
    {
        var order = CreatePaidOrder();
        order.StartInventoryFulfillment(Guid.NewGuid());
        order.CompleteInventoryFulfillment(Guid.NewGuid());
        return order;
    }

    private static Order CreateOrder(
        OrderStatus status,
        FulfillmentMode serviceFulfillmentMode = FulfillmentMode.InventoryCode)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"INV-{Guid.NewGuid():N}",
            UserId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            ServiceVariantId = Guid.NewGuid(),
            ServiceFulfillmentModeSnapshot = serviceFulfillmentMode,
            Status = status,
            CreatedAt = UtcNow.AddMinutes(-5)
        };
    }

    private static InventoryItem CreateAvailableItem(
        Guid serviceVariantId,
        string protectedCode = "protected-test-payload",
        string fingerprint = "test-fingerprint")
    {
        return InventoryItem.Create(
            serviceVariantId,
            FulfillmentMode.InventoryCode,
            protectedCode,
            fingerprint,
            Guid.NewGuid(),
            UtcNow.AddMinutes(-2));
    }

    private static InventoryItem CreateReservedItem(Order order)
    {
        var item = CreateAvailableItem(order.ServiceVariantId!.Value);
        item.ReserveFor(order.Id, UtcNow.AddMinutes(-1));
        return item;
    }

    private static InventoryItem CreateDeliveredItem(Order order)
    {
        var item = CreateReservedItem(order);
        item.MarkDelivered(order.Id, UtcNow.AddSeconds(-30));
        return item;
    }

    private sealed class FakeInventoryCodeFulfillmentRepository :
        IInventoryCodeFulfillmentRepository
    {
        public InventoryItem? ExistingAllocation { get; init; }
        public InventoryItem? NextAvailable { get; init; }
        public int ExistingAllocationCalls { get; private set; }
        public int LockCalls { get; private set; }
        public Guid? LockedServiceVariantId { get; private set; }

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
            LockCalls++;
            LockedServiceVariantId = serviceVariantId;
            return Task.FromResult(NextAvailable);
        }
    }

    private sealed record CommandSetup(
        FulfillInventoryCodeOrderCommand Command,
        FakeInventoryCodeFulfillmentRepository Repository,
        FakeNotificationService Notifications);
}

public sealed class InventoryCodeFulfillmentOutcomeTests
{
    [Fact]
    public void Outcomes_AreExplicitAndStable()
    {
        Assert.Equal(
            new[]
            {
                InventoryCodeFulfillmentOutcome.Fulfilled,
                InventoryCodeFulfillmentOutcome.AlreadyFulfilled,
                InventoryCodeFulfillmentOutcome.ResumedExistingAllocation,
                InventoryCodeFulfillmentOutcome.NoInventory,
                InventoryCodeFulfillmentOutcome.NotInventoryOrder,
                InventoryCodeFulfillmentOutcome.InvalidOrderState,
                InventoryCodeFulfillmentOutcome.InconsistentState
            },
            Enum.GetValues<InventoryCodeFulfillmentOutcome>());
    }
}
