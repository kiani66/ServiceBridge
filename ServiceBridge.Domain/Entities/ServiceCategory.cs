namespace ServiceBridge.Domain.Entities;

public class ServiceCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ICollection<DigitalService> Services { get; set; } = new List<DigitalService>();
    public ICollection<ProfitMargin> ProfitMargins { get; set; } = new List<ProfitMargin>();
}
