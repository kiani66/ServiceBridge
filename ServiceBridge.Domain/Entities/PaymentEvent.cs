using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Entities;

public class PaymentEvent : BaseEntity
{
    public Guid PaymentId { get; internal set; }
    public PaymentEventType EventType { get; internal set; }
    public string? GatewayReference { get; internal set; }
    public string? Payload { get; internal set; }
    public string? Note { get; internal set; }

    public Payment Payment { get; set; } = null!;
}
