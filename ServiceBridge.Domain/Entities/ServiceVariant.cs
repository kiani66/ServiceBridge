namespace ServiceBridge.Domain.Entities;

using ServiceBridge.Domain.Enums;

public class ServiceVariant : BaseEntity
{
    public Guid ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int? DurationDays { get; set; }
    public BillingCycle BillingCycle { get; set; } = BillingCycle.None;
    public AccountDeliveryType AccountDeliveryType { get; set; } =
        AccountDeliveryType.NotApplicable;
    public string RegionCode { get; set; } = "GLOBAL";
    public string CurrencyCode { get; set; } = "USD";
    public FulfillmentMode FulfillmentMode { get; set; } = FulfillmentMode.ManualAgent;
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string? ProcessingTimeDescription { get; set; }

    public DigitalService Service { get; set; } = null!;
}
