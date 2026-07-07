namespace ServiceBridge.Application.DTOs;

public sealed class OrderUpdatedRealtimeMessage
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public Guid UserId { get; set; }
    public Guid? AgentId { get; set; }
}
