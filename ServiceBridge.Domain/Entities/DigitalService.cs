namespace ServiceBridge.Domain.Entities;

public class DigitalService : BaseEntity
{
    public Guid CategoryId { get; set; }
    public Guid ProviderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public bool SupportsUseExistingAccount { get; set; } = true;
    public bool SupportsCreateNewAccount { get; set; }
    public string? ProcessingTimeDescription { get; set; }

    public ServiceCategory Category { get; set; } = null!;
    public Provider Provider { get; set; } = null!;
    public ICollection<ServiceVariant> Variants { get; set; } = new List<ServiceVariant>();
    public ICollection<ServiceRequiredField> RequiredFields { get; set; } =
        new List<ServiceRequiredField>();
    public ICollection<ProfitMargin> ProfitMargins { get; set; } = new List<ProfitMargin>();
}
