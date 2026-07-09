using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Audit;

public sealed class AuditLogEntry
{
    public Guid? ActorId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public AuditActionType ActionType { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public object? Before { get; set; }
    public object? After { get; set; }
    public string? Description { get; set; }
}
