namespace ServiceBridge.Domain.Exceptions;

public sealed class InvalidOrderFulfillmentOperationException : InvalidOperationException
{
    public InvalidOrderFulfillmentOperationException()
        : base("Agent assignment is only valid for ManualAgent fulfillment.")
    {
    }
}
