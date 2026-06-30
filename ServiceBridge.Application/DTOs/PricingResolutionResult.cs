namespace ServiceBridge.Application.DTOs;

public class PricingResolutionResult
{
    public Guid? ServiceVariantId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public decimal ForeignPrice { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal MarginPercentage
    {
        get => MarginPercent;
        set => MarginPercent = value;
    }

    public decimal BaseAmountIrr { get; set; }
    public decimal ProfitAmountIrr { get; set; }
    public decimal FinalAmountIrr { get; set; }
    public string PricingSource { get; set; } = string.Empty;
    public string ExchangeRateSource { get; set; } = string.Empty;
    public string MarginSource { get; set; } = string.Empty;
}
