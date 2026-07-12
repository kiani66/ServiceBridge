using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public sealed class AdminInventoryItemDto
{
    public Guid InventoryItemId { get; set; }
    public Guid ServiceVariantId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public InventoryItemStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByAdminName { get; set; } = string.Empty;
    public Guid? ReservedForOrderId { get; set; }
    public DateTime? ReservedAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime? DisabledAtUtc { get; set; }
    public string? DisabledByAdminName { get; set; }
}
