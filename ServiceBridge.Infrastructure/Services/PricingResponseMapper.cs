using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Services;

internal static class PricingResponseMapper
{
    public static ExchangeRateResponse MapExchangeRate(ExchangeRate exchangeRate)
    {
        return new ExchangeRateResponse
        {
            Id = exchangeRate.Id,
            CurrencyCode = exchangeRate.CurrencyCode,
            RateToIrr = exchangeRate.RateToIrr,
            IsActive = exchangeRate.IsActive,
            EffectiveFromUtc = exchangeRate.EffectiveFrom,
            CreatedAt = exchangeRate.CreatedAt
        };
    }

    public static ProfitMarginResponse MapProfitMargin(ProfitMargin margin)
    {
        return new ProfitMarginResponse
        {
            Id = margin.Id,
            ServiceVariantId = margin.ServiceVariantId,
            CategoryId = margin.CategoryId,
            ServiceId = margin.ServiceId,
            Scope = margin.ServiceVariantId.HasValue
                ? "ServiceVariant"
                : margin.ServiceId.HasValue
                    ? "Service"
                    : margin.CategoryId.HasValue
                    ? "Category"
                    : "Global",
            Percentage = margin.Percentage,
            IsActive = margin.IsActive,
            EffectiveFromUtc = margin.EffectiveFrom,
            CreatedAt = margin.CreatedAt
        };
    }
}
