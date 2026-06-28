using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Exceptions;

public sealed class InvalidOrderTransitionException : InvalidOperationException
{
    public InvalidOrderTransitionException(
        OrderStatus currentStatus,
        OrderStatus requestedStatus)
        : base($"Order cannot transition from {currentStatus} to {requestedStatus}.")
    {
        CurrentStatus = currentStatus;
        RequestedStatus = requestedStatus;
    }

    public OrderStatus CurrentStatus { get; }
    public OrderStatus RequestedStatus { get; }
}
