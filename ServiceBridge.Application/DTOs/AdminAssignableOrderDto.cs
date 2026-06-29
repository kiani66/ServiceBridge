namespace ServiceBridge.Application.DTOs;

public class AdminAssignableOrderDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public Guid? ServiceVariantId { get; set; }
    public string? PlanName { get; set; }
    public int? PlanDurationDays { get; set; }
    public string? PlanRegionCode { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal FinalAmountIrr { get; set; }
    public DateTime PaidAt { get; set; }
}
