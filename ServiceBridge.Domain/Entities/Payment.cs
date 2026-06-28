using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Entities;

public class Payment : BaseEntity
{
    private readonly List<PaymentEvent> _events = new();

    public Guid OrderId { get; internal set; }
    public decimal AmountIRR { get; internal set; }
    public decimal AmountUSD { get; internal set; }
    public decimal ExchangeRateSnapshot { get; internal set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? GatewayReference { get; private set; }
    public string? CallbackPayload { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    public Order Order { get; set; } = null!;
    public IReadOnlyCollection<PaymentEvent> Events => _events.AsReadOnly();

    public void RecordInitiated()
    {
        if (_events.Count > 0)
        {
            throw new InvalidOperationException(
                "Payment initiation has already been recorded.");
        }

        _events.Add(new PaymentEvent
        {
            PaymentId = Id,
            EventType = PaymentEventType.Initiated,
            CreatedAt = CreatedAt
        });
    }

    public void Complete(
        string? gatewayReference,
        string? callbackPayload,
        DateTime confirmedAtUtc)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("Only pending payments can be completed.");
        }

        Status = PaymentStatus.Completed;
        GatewayReference = string.IsNullOrWhiteSpace(gatewayReference)
            ? null
            : gatewayReference.Trim();
        CallbackPayload = callbackPayload;
        ConfirmedAt = confirmedAtUtc;
        UpdatedAt = confirmedAtUtc;

        _events.Add(new PaymentEvent
        {
            PaymentId = Id,
            EventType = PaymentEventType.Completed,
            GatewayReference = GatewayReference,
            Payload = callbackPayload,
            CreatedAt = confirmedAtUtc
        });
    }

    public void Fail(
        string? gatewayReference,
        string? callbackPayload,
        string failureReason,
        DateTime failedAtUtc)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("Only pending payments can fail.");
        }

        if (string.IsNullOrWhiteSpace(failureReason))
        {
            throw new InvalidOperationException("Payment failure reason is required.");
        }

        Status = PaymentStatus.Failed;
        GatewayReference = string.IsNullOrWhiteSpace(gatewayReference)
            ? null
            : gatewayReference.Trim();
        CallbackPayload = callbackPayload;
        FailedAt = failedAtUtc;
        UpdatedAt = failedAtUtc;

        _events.Add(new PaymentEvent
        {
            PaymentId = Id,
            EventType = PaymentEventType.Failed,
            GatewayReference = GatewayReference,
            Payload = callbackPayload,
            Note = failureReason.Trim(),
            CreatedAt = failedAtUtc
        });
    }

    public void Refund(
        string? gatewayReference,
        string? callbackPayload,
        string? note,
        DateTime refundedAtUtc)
    {
        if (Status != PaymentStatus.Completed)
        {
            throw new InvalidOperationException(
                "Only completed payments can be refunded.");
        }

        Status = PaymentStatus.Refunded;
        GatewayReference = string.IsNullOrWhiteSpace(gatewayReference)
            ? GatewayReference
            : gatewayReference.Trim();
        CallbackPayload = callbackPayload;
        RefundedAt = refundedAtUtc;
        UpdatedAt = refundedAtUtc;

        _events.Add(new PaymentEvent
        {
            PaymentId = Id,
            EventType = PaymentEventType.Refunded,
            GatewayReference = GatewayReference,
            Payload = callbackPayload,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = refundedAtUtc
        });
    }
}
