namespace ServiceBridge.Domain.Entities;

public class AgentAssignment : BaseEntity
{
    public Guid OrderId { get; internal set; }
    public Guid AgentId { get; internal set; }
    public Guid? AssignedByAdminId { get; internal set; }
    public DateTime AssignedAt { get; internal set; }
    public string? Notes { get; internal set; }

    public Order Order { get; set; } = null!;
    public User Agent { get; set; } = null!;
    public User? AssignedByAdmin { get; set; }
}
