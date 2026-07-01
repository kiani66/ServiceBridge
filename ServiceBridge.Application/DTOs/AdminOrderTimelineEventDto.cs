namespace ServiceBridge.Application.DTOs;

public class AdminOrderTimelineEventDto
{
    public Guid EventId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string ActorType { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public string? ActorEmail { get; set; }
    public string Tone { get; set; } = "neutral";
}
