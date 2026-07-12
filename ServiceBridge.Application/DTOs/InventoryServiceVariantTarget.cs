using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public sealed class InventoryServiceVariantTarget
{
    public Guid ServiceVariantId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public FulfillmentMode FulfillmentMode { get; set; }
}
