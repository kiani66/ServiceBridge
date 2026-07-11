namespace ServiceBridge.Application.DTOs;

public sealed class CreateInventoryItemsResponse
{
    public Guid ServiceVariantId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public int CreatedCount { get; set; }
}
