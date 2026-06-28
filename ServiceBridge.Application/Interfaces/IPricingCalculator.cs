using ServiceBridge.Application.DTOs;

namespace ServiceBridge.Application.Interfaces;

public interface IPricingCalculator
{
    PricingCalculation Calculate(
        decimal foreignPrice,
        decimal exchangeRate,
        decimal marginPercent);
}
