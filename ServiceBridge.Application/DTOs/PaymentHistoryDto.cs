namespace ServiceBridge.Application.DTOs;

public class PaymentHistoryDto
{
    public Guid PaymentId { get; set; }
    public decimal AmountIrr { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? GatewayReference { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
