namespace ServiceBridge.Application.DTOs;

public class AdminOrderListItemDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public Guid? ServiceVariantId { get; set; }
    public string? PlanName { get; set; }
    public int? PlanDurationDays { get; set; }
    public string? PlanRegionCode { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal? PaymentAmountIrr { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? GatewayReference { get; set; }
    public string? RefundStatus { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public Guid? AssignedAgentId { get; set; }
    public string? AssignedAgentName { get; set; }
    public DateTime CreatedAt { get; set; }
}
