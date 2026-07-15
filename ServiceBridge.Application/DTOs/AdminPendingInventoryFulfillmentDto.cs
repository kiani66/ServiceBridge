namespace ServiceBridge.Application.DTOs;

public class AdminPendingInventoryFulfillmentDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public AdminInventoryFulfillmentState InventoryFulfillmentState { get; set; }
    public string? InventoryAllocationStatus { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool CanRetryInventoryFulfillment { get; set; }
}
