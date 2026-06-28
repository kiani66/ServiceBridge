namespace ServiceBridge.Domain.Enums;

/// <summary>
/// Describes the fulfillment channel for a catalog item.
/// ExternalAutomation is reserved for future use and has no runtime behavior in PR4A.
/// </summary>
public enum FulfillmentMode
{
    ManualAgent = 1,
    InventoryCode = 2,
    ExternalAutomation = 3,
    InstructionOnly = 4
}
