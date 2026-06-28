namespace ServiceBridge.Domain.Entities;

public class ExchangeRate : BaseEntity
{
    public string CurrencyCode { get; set; } = "USD";
    public decimal RateToIrr { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
}
