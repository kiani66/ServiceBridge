namespace ServiceBridge.Application.DTOs;

public sealed class CustomerActionChangedRealtimeDto
{
    public Guid OrderId { get; set; }
    public Guid RequestId { get; set; }
    public string LinkUrl { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}
