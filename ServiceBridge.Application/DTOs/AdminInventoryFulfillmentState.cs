namespace ServiceBridge.Application.DTOs;

public enum AdminInventoryFulfillmentState
{
    NotApplicable,
    AwaitingStock,
    RecoverableAllocation,
    Fulfilled,
    Inconsistent
}
