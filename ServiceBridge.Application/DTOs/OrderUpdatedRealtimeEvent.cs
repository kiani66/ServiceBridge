namespace ServiceBridge.Application.DTOs;

public sealed class OrderUpdatedRealtimeEvent
{
    public Guid OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public Guid UserId { get; set; }
    public Guid? AgentId { get; set; }
    public bool NotifyAdmins { get; set; }
}
