namespace ServiceBridge.Application.DTOs;

public class PaymentInitiatedResponse
{
    public Guid PaymentId { get; set; }
    public string? Provider { get; set; }
    public string? GatewayReference { get; set; }
    public string? RedirectUrl { get; set; }
    public bool RequiresRedirect { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}
