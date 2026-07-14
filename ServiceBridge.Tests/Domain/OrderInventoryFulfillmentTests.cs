using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.Exceptions;
using Xunit;

namespace ServiceBridge.Tests.Orders;

public class OrderInventoryFulfillmentTests
{
    [Fact]
    public void StartInventoryFulfillment_WhenPaidInventoryCodeOrder_TransitionsAsSystem()
    {
        var order = CreatePaidOrder(FulfillmentMode.InventoryCode);
        var systemActorId = Guid.NewGuid();

        order.StartInventoryFulfillment(systemActorId, "Inventory code reserved.");

        Assert.Equal(OrderStatus.Processing, order.Status);
        var history = Assert.Single(order.StatusHistory.Where(x =>
            x.NewStatus == OrderStatus.Processing));
        Assert.Equal(OrderStatus.Paid, history.PreviousStatus);
        Assert.Equal(OrderLogActorType.System, history.ActorType);
        Assert.Equal(systemActorId, history.ActorId);
        Assert.Null(history.ChangedByUserId);
    }

    [Fact]
    public void CompleteInventoryFulfillment_WhenProcessingInventoryCodeOrder_TransitionsAsSystem()
    {
        var order = CreatePaidOrder(FulfillmentMode.InventoryCode);
        var systemActorId = Guid.NewGuid();
        order.StartInventoryFulfillment(systemActorId);

        order.CompleteInventoryFulfillment(systemActorId, "Inventory code delivered.");

        Assert.Equal(OrderStatus.Completed, order.Status);
        var history = Assert.Single(order.StatusHistory.Where(x =>
            x.NewStatus == OrderStatus.Completed));
        Assert.Equal(OrderStatus.Processing, history.PreviousStatus);
        Assert.Equal(OrderLogActorType.System, history.ActorType);
        Assert.Equal(systemActorId, history.ActorId);
        Assert.Null(history.ChangedByUserId);
    }

    [Fact]
    public void InventoryFulfillment_InvalidTransitions_ThrowInvalidOrderTransitionException()
    {
        var manualPaidOrder = CreatePaidOrder(FulfillmentMode.ManualAgent);
        var inventoryCreatedOrder = new Order
        {
            ServiceFulfillmentModeSnapshot = FulfillmentMode.InventoryCode,
            Status = OrderStatus.Created
        };
        var inventoryAssignedOrder = CreatePaidOrder(FulfillmentMode.InventoryCode);
        inventoryAssignedOrder.AssignToAgent(Guid.NewGuid(), Guid.NewGuid(), null, "Assigned.");
        var inventoryPaidOrder = CreatePaidOrder(FulfillmentMode.InventoryCode);
        var inventoryCompletedOrder = CreatePaidOrder(FulfillmentMode.InventoryCode);
        inventoryCompletedOrder.StartInventoryFulfillment(Guid.NewGuid());
        inventoryCompletedOrder.CompleteInventoryFulfillment(Guid.NewGuid());

        Assert.Throws<InvalidOrderTransitionException>(
            () => manualPaidOrder.StartInventoryFulfillment(Guid.NewGuid()));
        Assert.Throws<InvalidOrderTransitionException>(
            () => manualPaidOrder.CompleteInventoryFulfillment(Guid.NewGuid()));
        Assert.Throws<InvalidOrderTransitionException>(
            () => inventoryCreatedOrder.StartInventoryFulfillment(Guid.NewGuid()));
        Assert.Throws<InvalidOrderTransitionException>(
            () => inventoryAssignedOrder.StartInventoryFulfillment(Guid.NewGuid()));
        Assert.Throws<InvalidOrderTransitionException>(
            () => inventoryPaidOrder.CompleteInventoryFulfillment(Guid.NewGuid()));
        Assert.Throws<InvalidOrderTransitionException>(
            () => inventoryCompletedOrder.StartInventoryFulfillment(Guid.NewGuid()));
        Assert.Throws<InvalidOrderTransitionException>(
            () => inventoryCompletedOrder.CompleteInventoryFulfillment(Guid.NewGuid()));
    }

    [Fact]
    public void ManualAgentFlow_RemainsAssignmentDependent_AndGenericPaidProcessingIsBlocked()
    {
        var order = CreatePaidOrder(FulfillmentMode.ManualAgent);
        var assignedAgentId = Guid.NewGuid();

        Assert.False(order.CanTransitionTo(OrderStatus.Processing));
        Assert.Throws<InvalidOrderTransitionException>(() => order.TransitionTo(
            OrderStatus.Processing,
            Guid.NewGuid(),
            DateTime.UtcNow));
        Assert.Throws<InvalidOperationException>(
            () => order.StartProcessing(Guid.NewGuid()));

        order.AssignToAgent(assignedAgentId, Guid.NewGuid(), null, "Assigned.");
        order.StartProcessing(assignedAgentId);

        Assert.Throws<InvalidOperationException>(
            () => order.Complete(Guid.NewGuid()));

        order.Complete(assignedAgentId);

        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    private static Order CreatePaidOrder(FulfillmentMode serviceFulfillmentMode)
    {
        var order = new Order
        {
            ServiceFulfillmentModeSnapshot = serviceFulfillmentMode,
            Status = OrderStatus.Created
        };

        order.MoveToWaitingForPayment(Guid.NewGuid(), DateTime.UtcNow);
        order.MarkPaid(Guid.NewGuid(), "Payment confirmed.");

        return order;
    }
}
