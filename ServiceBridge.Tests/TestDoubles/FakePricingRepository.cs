using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakePricingRepository : IPricingRepository
{
    public ServicePricingContext? ServiceContext { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal? ServiceMargin { get; set; }
    public decimal? CategoryMargin { get; set; }
    public decimal? GlobalMargin { get; set; }

    public int ServiceContextCallCount { get; private set; }
    public int ExchangeRateCallCount { get; private set; }
    public int ServiceMarginCallCount { get; private set; }
    public int CategoryMarginCallCount { get; private set; }
    public int GlobalMarginCallCount { get; private set; }

    public Task<ServicePricingContext?> GetActiveServiceContextAsync(
        Guid serviceId,
        Guid? planId = null)
    {
        ServiceContextCallCount++;
        if (ServiceContext != null && planId.HasValue)
        {
            ServiceContext.PlanId = planId;
        }

        return Task.FromResult(ServiceContext);
    }

    public Task<decimal?> GetEffectiveExchangeRateAsync(
        string currencyCode,
        DateTime asOfUtc)
    {
        ExchangeRateCallCount++;
        return Task.FromResult(ExchangeRate);
    }

    public Task<decimal?> GetEffectiveServiceMarginAsync(
        Guid serviceId,
        DateTime asOfUtc)
    {
        ServiceMarginCallCount++;
        return Task.FromResult(ServiceMargin);
    }

    public Task<decimal?> GetEffectiveCategoryMarginAsync(
        Guid categoryId,
        DateTime asOfUtc)
    {
        CategoryMarginCallCount++;
        return Task.FromResult(CategoryMargin);
    }

    public Task<decimal?> GetEffectiveGlobalMarginAsync(DateTime asOfUtc)
    {
        GlobalMarginCallCount++;
        return Task.FromResult(GlobalMargin);
    }
}
