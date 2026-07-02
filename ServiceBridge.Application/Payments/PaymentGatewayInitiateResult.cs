namespace ServiceBridge.Application.Payments;

public sealed class PaymentGatewayInitiateResult
{
    public required string Provider { get; init; }
    public required Guid PaymentId { get; init; }
    public string? GatewayReference { get; init; }
    public string? RedirectUrl { get; init; }
    public bool RequiresRedirect { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
}
