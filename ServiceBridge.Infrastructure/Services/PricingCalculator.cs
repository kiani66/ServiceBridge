using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Infrastructure.Services;

public class PricingCalculator : IPricingCalculator
{
    public PricingCalculation Calculate(
        decimal foreignPrice,
        decimal exchangeRate,
        decimal marginPercent)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(foreignPrice);

        if (exchangeRate <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(exchangeRate),
                "Exchange rate must be greater than zero.");
        }

        if (marginPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(marginPercent),
                "Margin percent must be between 0 and 100.");
        }

        var baseAmount = RoundIrr(foreignPrice * exchangeRate);
        var profitAmount = RoundIrr(baseAmount * marginPercent / 100m);

        return new PricingCalculation
        {
            ForeignPrice = foreignPrice,
            ExchangeRate = exchangeRate,
            MarginPercent = marginPercent,
            BaseAmountIrr = baseAmount,
            ProfitAmountIrr = profitAmount,
            FinalAmountIrr = baseAmount + profitAmount
        };
    }

    private static decimal RoundIrr(decimal amount)
    {
        return decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
    }
}
