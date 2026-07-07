namespace ServiceBridge.Application.DTOs;

public class AdminRefundRequestDto : RefundRequestDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? ReviewedByAdminName { get; set; }
}
