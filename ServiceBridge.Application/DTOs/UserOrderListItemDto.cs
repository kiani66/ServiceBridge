namespace ServiceBridge.Application.DTOs;

public class UserOrderListItemDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string FulfillmentMode { get; set; } = string.Empty;
    public Guid? ServiceVariantId { get; set; }
    public string? PlanName { get; set; }
    public int? PlanDurationDays { get; set; }
    public string? PlanRegionCode { get; set; }
    public decimal BasePriceUsdSnapshot { get; set; }
    public decimal FinalPriceIrr { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool CanCancel { get; set; }
}
