namespace ServiceBridge.Application.Inventory;

public enum InventoryCodeFulfillmentOutcome
{
    Fulfilled = 1,
    AlreadyFulfilled = 2,
    ResumedExistingAllocation = 3,
    NoInventory = 4,
    NotInventoryOrder = 5,
    InvalidOrderState = 6,
    InconsistentState = 7
}
