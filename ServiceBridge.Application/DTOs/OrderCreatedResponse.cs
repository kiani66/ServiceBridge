using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public class OrderCreatedResponse
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public OrderFulfillmentMode FulfillmentMode { get; set; }
    public Guid? ServiceVariantId { get; set; }
    public Guid? PlanId { get; set; }
    public string? PlanName { get; set; }
    public int? PlanDurationDays { get; set; }
    public string? PlanRegionCode { get; set; }
    public decimal ForeignPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal MarginPercentage { get; set; }
    public decimal FinalAmountIrr { get; set; }
    public DateTime CreatedAt { get; set; }
}
