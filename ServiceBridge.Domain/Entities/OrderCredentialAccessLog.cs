namespace ServiceBridge.Domain.Entities;

public class OrderCredentialAccessLog : BaseEntity
{
    public Guid OrderId { get; internal set; }
    public Guid AccessedByUserId { get; internal set; }
    public string AccessType { get; internal set; } = string.Empty;

    public Order Order { get; set; } = null!;
    public User AccessedByUser { get; set; } = null!;
}
