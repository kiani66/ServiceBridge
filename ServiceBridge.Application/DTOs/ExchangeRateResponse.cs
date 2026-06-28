namespace ServiceBridge.Application.DTOs;

public class ExchangeRateResponse
{
    public Guid Id { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal RateToIrr { get; set; }
    public bool IsActive { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime CreatedAt { get; set; }
}
