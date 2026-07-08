namespace ServiceBridge.Application.DTOs;

public sealed class OrderUpdatedRealtimeDto
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}
