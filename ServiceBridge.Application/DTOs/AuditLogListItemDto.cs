namespace ServiceBridge.Application.DTOs;

public class AuditLogListItemDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid? ActorId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? Description { get; set; }
}
