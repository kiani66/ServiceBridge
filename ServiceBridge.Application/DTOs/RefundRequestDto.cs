namespace ServiceBridge.Application.DTOs;

public class RefundRequestDto
{
    public Guid RefundRequestId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? AdminNote { get; set; }
}
