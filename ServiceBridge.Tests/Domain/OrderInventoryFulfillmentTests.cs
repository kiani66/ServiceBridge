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
        var inventoryAssignedOrder = new Order
        {
            ServiceFulfillmentModeSnapshot = FulfillmentMode.InventoryCode,
            Status = OrderStatus.Assigned
        };
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

    [Theory]
    [InlineData(FulfillmentMode.InventoryCode)]
    [InlineData(FulfillmentMode.ManualAgent)]
    public void GenericAssignmentTransition_WhenPaidOrder_RejectsWithoutMutation(
        FulfillmentMode fulfillmentMode)
    {
        var order = CreatePaidOrder(fulfillmentMode);
        var status = order.Status;
        var assignedAgentId = order.AssignedAgentId;
        var paidAt = order.PaidAt;
        var assignedAt = order.AssignedAt;
        var completedAt = order.CompletedAt;
        var cancelledAt = order.CancelledAt;
        var updatedAt = order.UpdatedAt;
        var assignmentCount = order.Assignments.Count;
        var historyCount = order.StatusHistory.Count;

        Assert.False(order.CanTransitionTo(OrderStatus.Assigned));
        Assert.Throws<InvalidOrderTransitionException>(() => order.TransitionTo(
            OrderStatus.Assigned,
            Guid.NewGuid(),
            DateTime.UtcNow,
            "Generic assignment."));

        Assert.Equal(status, order.Status);
        Assert.Equal(assignedAgentId, order.AssignedAgentId);
        Assert.Equal(paidAt, order.PaidAt);
        Assert.Equal(assignedAt, order.AssignedAt);
        Assert.Equal(completedAt, order.CompletedAt);
        Assert.Equal(cancelledAt, order.CancelledAt);
        Assert.Equal(updatedAt, order.UpdatedAt);
        Assert.Equal(assignmentCount, order.Assignments.Count);
        Assert.Equal(historyCount, order.StatusHistory.Count);
    }

    [Fact]
    public void Assignment_WhenPaidManualAgentOrder_SetsAssignmentAndWritesOneHistoryEntry()
    {
        var order = CreatePaidOrder(FulfillmentMode.ManualAgent);
        var agentId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var historyCount = order.StatusHistory.Count;

        order.AssignToAgent(
            agentId,
            adminId,
            "  Assignment notes  ",
            "Assigned by Admin.");

        Assert.Equal(OrderStatus.Assigned, order.Status);
        Assert.Equal(agentId, order.AssignedAgentId);
        Assert.NotNull(order.AssignedAt);
        Assert.Equal(order.AssignedAt, order.UpdatedAt);

        var assignment = Assert.Single(order.Assignments);
        Assert.Equal(agentId, assignment.AgentId);
        Assert.Equal(adminId, assignment.AssignedByAdminId);
        Assert.Equal(order.AssignedAt, assignment.AssignedAt);
        Assert.Equal("Assignment notes", assignment.Notes);

        Assert.Equal(historyCount + 1, order.StatusHistory.Count);
        var history = Assert.Single(order.StatusHistory.Where(x =>
            x.NewStatus == OrderStatus.Assigned));
        Assert.Equal(OrderStatus.Paid, history.PreviousStatus);
        Assert.Equal(OrderLogActorType.Admin, history.ActorType);
        Assert.Equal(adminId, history.ActorId);
        Assert.Equal(adminId, history.ChangedByUserId);
        Assert.Equal("Assigned by Admin.", history.Note);
        Assert.Equal(order.AssignedAt, history.CreatedAt);
    }

    [Fact]
    public void GenericNonAssignmentTransition_WhenAllowed_RemainsAvailable()
    {
        var order = new Order { Status = OrderStatus.Created };
        var userId = Guid.NewGuid();
        var changedAtUtc = DateTime.UtcNow;

        Assert.True(order.CanTransitionTo(OrderStatus.WaitingForPayment));

        order.TransitionTo(
            OrderStatus.WaitingForPayment,
            userId,
            changedAtUtc,
            "Ready for payment.");

        Assert.Equal(OrderStatus.WaitingForPayment, order.Status);
        Assert.Equal(changedAtUtc, order.UpdatedAt);
        var history = Assert.Single(order.StatusHistory);
        Assert.Equal(OrderStatus.Created, history.PreviousStatus);
        Assert.Equal(OrderStatus.WaitingForPayment, history.NewStatus);
        Assert.Equal(OrderLogActorType.User, history.ActorType);
        Assert.Equal(userId, history.ActorId);
        Assert.Equal("Ready for payment.", history.Note);
    }

    [Fact]
    public void Assignment_WhenInventoryCodeOrder_RejectsWithoutMutation()
    {
        var order = CreatePaidOrder(FulfillmentMode.InventoryCode);
        var status = order.Status;
        var assignedAgentId = order.AssignedAgentId;
        var assignedAt = order.AssignedAt;
        var updatedAt = order.UpdatedAt;
        var assignmentCount = order.Assignments.Count;
        var historyCount = order.StatusHistory.Count;

        Assert.Throws<InvalidOrderFulfillmentOperationException>(() => order.AssignToAgent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Assignment notes",
            "Assigned."));

        Assert.Equal(status, order.Status);
        Assert.Equal(assignedAgentId, order.AssignedAgentId);
        Assert.Equal(assignedAt, order.AssignedAt);
        Assert.Equal(updatedAt, order.UpdatedAt);
        Assert.Equal(assignmentCount, order.Assignments.Count);
        Assert.Equal(historyCount, order.StatusHistory.Count);
    }

    [Fact]
    public void Assignment_WhenManualAgentOrderIsNotPaid_RejectsWithoutMutation()
    {
        var order = new Order
        {
            ServiceFulfillmentModeSnapshot = FulfillmentMode.ManualAgent,
            Status = OrderStatus.WaitingForPayment
        };
        var status = order.Status;
        var assignedAgentId = order.AssignedAgentId;
        var assignedAt = order.AssignedAt;
        var updatedAt = order.UpdatedAt;
        var assignmentCount = order.Assignments.Count;
        var historyCount = order.StatusHistory.Count;

        Assert.Throws<InvalidOrderTransitionException>(() => order.AssignToAgent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Assignment notes",
            "Assigned."));

        Assert.Equal(status, order.Status);
        Assert.Equal(assignedAgentId, order.AssignedAgentId);
        Assert.Equal(assignedAt, order.AssignedAt);
        Assert.Equal(updatedAt, order.UpdatedAt);
        Assert.Equal(assignmentCount, order.Assignments.Count);
        Assert.Equal(historyCount, order.StatusHistory.Count);
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
