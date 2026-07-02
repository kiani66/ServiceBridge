using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Payments;

public sealed class PaymentGatewayVerifyResult
{
    public required string Provider { get; init; }
    public required bool IsVerified { get; init; }
    public Guid? PaymentId { get; init; }
    public PaymentStatus Status { get; init; } = PaymentStatus.Failed;
    public string? GatewayReference { get; init; }
    public string? FailureReason { get; init; }
    public required string SanitizedPayload { get; init; }
}
