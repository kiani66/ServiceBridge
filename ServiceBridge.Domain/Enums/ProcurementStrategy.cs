namespace ServiceBridge.Domain.Enums;

public enum ProcurementStrategy
{
    NotRequired = 1,
    PreStockedInventory = 2,
    OnDemandManual = 3,
    OnDemandSupplierApi = 4,

    /// <summary>
    /// Attempts inventory first; if unavailable, a future coordinator continues
    /// fulfillment through the manual-agent path.
    /// </summary>
    PreferInventoryThenManual = 5
}
