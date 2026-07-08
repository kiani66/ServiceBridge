namespace ServiceBridge.Application.DTOs;

public sealed class CustomerActionChangedRealtimeEvent
{
    public Guid OrderId { get; set; }
    public Guid RequestId { get; set; }
    public string LinkUrl { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public Guid UserId { get; set; }
    public Guid? AgentId { get; set; }
}
