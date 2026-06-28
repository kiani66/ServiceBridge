namespace ServiceBridge.Domain.Entities;

public class Provider : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<DigitalService> Services { get; set; } = new List<DigitalService>();
}
