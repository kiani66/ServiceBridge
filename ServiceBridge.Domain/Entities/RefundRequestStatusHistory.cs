using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Entities;

public class RefundRequestStatusHistory : BaseEntity
{
    public Guid RefundRequestId { get; set; }
    public RefundStatus? PreviousStatus { get; set; }
    public RefundStatus NewStatus { get; set; }
    public OrderLogActorType ActorType { get; set; }
    public Guid ActorId { get; set; }
    public string? Note { get; set; }

    public RefundRequest RefundRequest { get; set; } = null!;
}
