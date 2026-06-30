namespace ServiceBridge.Application.DTOs;

public class ProfitMarginResponse
{
    public Guid Id { get; set; }
    public Guid? ServiceVariantId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ServiceId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public bool IsActive { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime CreatedAt { get; set; }
}
