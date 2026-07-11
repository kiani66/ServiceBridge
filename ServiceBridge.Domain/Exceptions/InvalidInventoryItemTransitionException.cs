using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Exceptions;

public sealed class InvalidInventoryItemTransitionException : InvalidOperationException
{
    public InvalidInventoryItemTransitionException(
        InventoryItemStatus currentStatus,
        InventoryItemStatus requestedStatus)
        : base(
            $"Inventory item cannot transition from {currentStatus} to {requestedStatus}.")
    {
        CurrentStatus = currentStatus;
        RequestedStatus = requestedStatus;
    }

    public InventoryItemStatus CurrentStatus { get; }
    public InventoryItemStatus RequestedStatus { get; }
}
