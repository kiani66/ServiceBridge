namespace ServiceBridge.Application.DTOs;

public sealed class TicketUpdatedRealtimeDto
{
    public Guid TicketId { get; set; }
    public string LinkUrl { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}
