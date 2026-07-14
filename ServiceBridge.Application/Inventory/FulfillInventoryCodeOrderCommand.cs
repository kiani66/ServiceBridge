using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Inventory;

public sealed class FulfillInventoryCodeOrderCommand
{
    private const string ReservedNote = "Inventory code reserved.";
    private const string DeliveredNote = "Inventory code delivered.";

    private readonly IInventoryCodeFulfillmentRepository _repository;
    private readonly INotificationService _notificationService;

    public FulfillInventoryCodeOrderCommand(
        IInventoryCodeFulfillmentRepository repository,
        INotificationService notificationService)
    {
        _repository = repository;
        _notificationService = notificationService;
    }

    public async Task<ServiceResult<InventoryCodeFulfillmentOutcome>> ExecuteAsync(
        Order order,
        Guid systemActorId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (systemActorId == Guid.Empty)
        {
            throw new ArgumentException("System actor ID is required.", nameof(systemActorId));
        }

        if (utcNow == default)
        {
            throw new ArgumentException("Current UTC time is required.", nameof(utcNow));
        }

        if (order.ServiceFulfillmentModeSnapshot != FulfillmentMode.InventoryCode)
        {
            return Outcome(InventoryCodeFulfillmentOutcome.NotInventoryOrder);
        }

        var existingAllocation = await _repository.GetExistingAllocationAsync(
            order.Id,
            cancellationToken);

        if (existingAllocation is not null)
        {
            return ResumeExistingAllocation(
                order,
                existingAllocation,
                systemActorId,
                utcNow);
        }

        if (order.Status != OrderStatus.Paid)
        {
            return Outcome(InventoryCodeFulfillmentOutcome.InvalidOrderState);
        }

        if (order.ServiceVariantId is not { } serviceVariantId ||
            serviceVariantId == Guid.Empty)
        {
            return Outcome(InventoryCodeFulfillmentOutcome.InconsistentState);
        }

        var item = await _repository.LockNextAvailableAsync(
            serviceVariantId,
            cancellationToken);
        if (item is null)
        {
            return Outcome(InventoryCodeFulfillmentOutcome.NoInventory);
        }

        item.ReserveFor(order.Id, utcNow);
        order.StartInventoryFulfillment(systemActorId, ReservedNote);
        item.MarkDelivered(order.Id, utcNow);
        order.CompleteInventoryFulfillment(systemActorId, DeliveredNote);

        await _notificationService.QueueOrderCompletedAsync(
            order.UserId,
            order.Id,
            order.OrderNumber);

        return Outcome(InventoryCodeFulfillmentOutcome.Fulfilled);
    }

    private static ServiceResult<InventoryCodeFulfillmentOutcome>
        ResumeExistingAllocation(
            Order order,
            InventoryItem item,
            Guid systemActorId,
            DateTime utcNow)
    {
        if (item.ReservedForOrderId != order.Id)
        {
            return Outcome(InventoryCodeFulfillmentOutcome.InconsistentState);
        }

        if (item.Status == InventoryItemStatus.Reserved)
        {
            if (order.Status is not (OrderStatus.Paid or OrderStatus.Processing))
            {
                return Outcome(InventoryCodeFulfillmentOutcome.InconsistentState);
            }

            if (order.Status == OrderStatus.Paid)
            {
                order.StartInventoryFulfillment(systemActorId, ReservedNote);
            }

            item.MarkDelivered(order.Id, utcNow);
            order.CompleteInventoryFulfillment(systemActorId, DeliveredNote);

            return Outcome(
                InventoryCodeFulfillmentOutcome.ResumedExistingAllocation);
        }

        if (item.Status == InventoryItemStatus.Delivered)
        {
            if (order.Status == OrderStatus.Completed)
            {
                return Outcome(InventoryCodeFulfillmentOutcome.AlreadyFulfilled);
            }

            if (order.Status == OrderStatus.Processing)
            {
                order.CompleteInventoryFulfillment(systemActorId, DeliveredNote);
                return Outcome(
                    InventoryCodeFulfillmentOutcome.ResumedExistingAllocation);
            }
        }

        return Outcome(InventoryCodeFulfillmentOutcome.InconsistentState);
    }

    private static ServiceResult<InventoryCodeFulfillmentOutcome> Outcome(
        InventoryCodeFulfillmentOutcome outcome)
    {
        return ServiceResult<InventoryCodeFulfillmentOutcome>.Ok(outcome);
    }
}
