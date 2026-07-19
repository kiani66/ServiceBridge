using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Exceptions;

public sealed class InvalidFulfillmentExecutionTransitionException :
    InvalidOperationException
{
    public InvalidFulfillmentExecutionTransitionException(
        FulfillmentExecutionStatus currentStatus,
        string operation)
        : base(
            $"Fulfillment execution cannot perform {operation} while it is {currentStatus}.")
    {
        CurrentStatus = currentStatus;
        Operation = operation;
    }

    public FulfillmentExecutionStatus CurrentStatus { get; }
    public string Operation { get; }
}
