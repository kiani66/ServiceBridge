namespace ServiceBridge.Application.DTOs;

public sealed class TicketUpdatedRealtimeEvent
{
    public Guid TicketId { get; set; }
    public string LinkUrl { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public Guid? UserId { get; set; }
    public bool NotifyAdmins { get; set; }
}
