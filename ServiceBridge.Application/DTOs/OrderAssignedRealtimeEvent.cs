namespace ServiceBridge.Application.DTOs;

public sealed class OrderAssignedRealtimeEvent
{
    public Guid OrderId { get; set; }
    public string LinkUrl { get; set; } = string.Empty;
    public DateTime AssignedAtUtc { get; set; }
    public Guid UserId { get; set; }
    public Guid AgentId { get; set; }
}
