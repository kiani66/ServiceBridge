namespace ServiceBridge.Application.DTOs;

public sealed class InventoryAvailabilitySummaryDto
{
    public Guid ServiceVariantId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public int Available { get; set; }
    public int Reserved { get; set; }
    public int Delivered { get; set; }
    public int Disabled { get; set; }
    public int Total { get; set; }
}
