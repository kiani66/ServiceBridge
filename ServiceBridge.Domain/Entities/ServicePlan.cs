namespace ServiceBridge.Domain.Entities;

public class ServicePlan : BaseEntity
{
    public Guid ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int? DurationDays { get; set; }
    public string? RegionCode { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string? ProcessingTimeDescription { get; set; }

    public DigitalService Service { get; set; } = null!;
}
