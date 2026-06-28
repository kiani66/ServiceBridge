using ServiceBridge.Application.DTOs;

namespace ServiceBridge.Application.Interfaces;

public interface IPricingRepository
{
    Task<ServicePricingContext?> GetActiveServiceContextAsync(
        Guid serviceId,
        Guid? planId = null);
    Task<decimal?> GetEffectiveExchangeRateAsync(string currencyCode, DateTime asOfUtc);
    Task<decimal?> GetEffectiveServiceMarginAsync(Guid serviceId, DateTime asOfUtc);
    Task<decimal?> GetEffectiveCategoryMarginAsync(Guid categoryId, DateTime asOfUtc);
    Task<decimal?> GetEffectiveGlobalMarginAsync(DateTime asOfUtc);
}
