namespace ServiceBridge.Application.Fulfillment;

public enum FulfillmentInitializationOutcome
{
    NotApplicable = 1,
    AlreadyExists = 2,
    Created = 3,
    LegacyModeMismatch = 4
}
