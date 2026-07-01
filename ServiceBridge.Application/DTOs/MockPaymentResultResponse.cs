using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public class MockPaymentResultResponse
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public PaymentStatus Status { get; set; }
    public string? GatewayReference { get; set; }
}
