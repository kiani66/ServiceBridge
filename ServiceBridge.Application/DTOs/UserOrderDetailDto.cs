namespace ServiceBridge.Application.DTOs;

public class UserOrderDetailDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string FulfillmentMode { get; set; } = string.Empty;
    public string? PlanName { get; set; }
    public int? PlanDurationDays { get; set; }
    public string? PlanRegionCode { get; set; }
    public decimal BasePriceUsdSnapshot { get; set; }
    public decimal ExchangeRateSnapshot { get; set; }
    public decimal MarginSnapshot { get; set; }
    public decimal FinalPriceIrr { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool CanCancel { get; set; }
    public IReadOnlyCollection<UserOrderFieldDto> Fields { get; set; } =
        Array.Empty<UserOrderFieldDto>();
    public IReadOnlyCollection<UserOrderFieldDto> DeliveryFields { get; set; } =
        Array.Empty<UserOrderFieldDto>();
    public IReadOnlyCollection<UserVisibleOrderNoteDto> Notes { get; set; } =
        Array.Empty<UserVisibleOrderNoteDto>();
    public IReadOnlyCollection<CustomerActionRequestDto> CustomerActions { get; set; } =
        Array.Empty<CustomerActionRequestDto>();
    public IReadOnlyCollection<PaymentHistoryDto> Payments { get; set; } =
        Array.Empty<PaymentHistoryDto>();
    public IReadOnlyCollection<OrderStatusHistoryDto> StatusHistory { get; set; } =
        Array.Empty<OrderStatusHistoryDto>();
}
