namespace ServiceBridge.Application.DTOs;

public class ServicePricingContext
{
    public Guid ServiceId { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? ServiceVariantId { get; set; }
    public Guid? PlanId
    {
        get => ServiceVariantId;
        set => ServiceVariantId = value;
    }

    public string? PlanName { get; set; }
    public int? PlanDurationDays { get; set; }
    public string? PlanRegionCode { get; set; }
    public string? RegionCode
    {
        get => PlanRegionCode;
        set => PlanRegionCode = value;
    }

    public string CurrencyCode { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public bool IsVariantActive { get; set; } = true;
    public bool IsServiceActive { get; set; } = true;
    public bool IsCategoryActive { get; set; } = true;
    public bool IsProviderActive { get; set; } = true;
}
