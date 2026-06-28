using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Entities;

public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; internal set; }
    public OrderStatus? PreviousStatus { get; internal set; }
    public OrderStatus NewStatus { get; internal set; }
    public OrderLogActorType ActorType { get; internal set; }
    public Guid ActorId { get; internal set; }
    public Guid? ChangedByUserId { get; internal set; }
    public string? Note { get; internal set; }

    public Order Order { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}
