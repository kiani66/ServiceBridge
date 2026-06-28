namespace ServiceBridge.Application.DTOs;

public class PricingQuoteResponse
{
    public Guid ServiceId { get; set; }
    public Guid? PlanId { get; set; }
    public string? PlanName { get; set; }
    public int? PlanDurationDays { get; set; }
    public string? PlanRegionCode { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ForeignPrice { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal MarginPercentage { get; set; }
    public decimal BaseAmountIrr { get; set; }
    public decimal ProfitAmountIrr { get; set; }
    public decimal FinalAmountIrr { get; set; }
    public DateTime QuotedAtUtc { get; set; }
}
