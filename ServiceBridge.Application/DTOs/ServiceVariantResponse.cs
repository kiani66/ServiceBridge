namespace ServiceBridge.Application.DTOs;

public class ServiceVariantResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int? DurationDays { get; set; }
    public string? RegionCode { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string? ProcessingTimeDescription { get; set; }
}
