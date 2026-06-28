namespace ServiceBridge.Application.DTOs;

public class ServicePricingContext
{
    public Guid ServiceId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? PlanId { get; set; }
    public string? PlanName { get; set; }
    public int? PlanDurationDays { get; set; }
    public string? PlanRegionCode { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
}
