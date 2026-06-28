namespace ServiceBridge.Domain.Entities;

public class ProfitMargin : BaseEntity
{
    public Guid? CategoryId { get; set; }
    public Guid? ServiceId { get; set; }
    public decimal Percentage { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    public ServiceCategory? Category { get; set; }
    public DigitalService? Service { get; set; }
}
