namespace ServiceBridge.Application.Fulfillment;

public enum FulfillmentHandlerOutcome
{
    Succeeded = 1,
    RetryableFailure = 2,
    TerminalFailure = 3
}
