namespace ServiceBridge.Application.DTOs;

public class AdminPaymentListItemDto
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal AmountIrr { get; set; }
    public string? TransactionNumber { get; set; }
    public DateTime PaymentDate { get; set; }
}
