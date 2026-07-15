using ServiceBridge.Application.Inventory;

namespace ServiceBridge.Application.DTOs;

public sealed class RetryInventoryCodeFulfillmentResultDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public InventoryCodeFulfillmentOutcome Outcome { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
}
