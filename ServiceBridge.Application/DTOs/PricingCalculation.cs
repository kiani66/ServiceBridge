namespace ServiceBridge.Application.DTOs;

public class PricingCalculation
{
    public decimal ForeignPrice { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal BaseAmountIrr { get; set; }
    public decimal ProfitAmountIrr { get; set; }
    public decimal FinalAmountIrr { get; set; }
}
