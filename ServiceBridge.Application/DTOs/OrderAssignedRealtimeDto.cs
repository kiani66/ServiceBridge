namespace ServiceBridge.Application.DTOs;

public sealed class OrderAssignedRealtimeDto
{
    public Guid OrderId { get; set; }
    public string LinkUrl { get; set; } = string.Empty;
    public DateTime AssignedAtUtc { get; set; }
}
