namespace ServiceBridge.Application.DTOs;

public sealed class RealtimeEventDto
{
    public string EventType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int? UnreadCount { get; set; }
}
