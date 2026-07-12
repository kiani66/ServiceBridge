namespace ServiceBridge.Application.DTOs;

public sealed class AdminInventoryVariantOptionDto
{
    public Guid ServiceVariantId { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
}
