using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class PricingRepository : IPricingRepository
{
    private readonly ServiceBridgeDbContext _context;

    public PricingRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public async Task<ServicePricingContext?> GetActiveServiceContextAsync(
        Guid serviceId,
        Guid? planId = null)
    {
        var serviceContext = await _context.DigitalServices
            .AsNoTracking()
            .Where(x =>
                x.Id == serviceId &&
                x.IsActive &&
                x.Category.IsActive)
            .Select(x => new ServicePricingContext
            {
                ServiceId = x.Id,
                CategoryId = x.CategoryId,
                CurrencyCode = x.CurrencyCode,
                BasePrice = x.BasePrice
            })
            .FirstOrDefaultAsync();

        if (serviceContext == null || !planId.HasValue)
        {
            return serviceContext;
        }

        var planContext = await _context.ServiceVariants
            .AsNoTracking()
            .Where(x =>
                x.Id == planId.Value &&
                x.ServiceId == serviceId &&
                x.IsActive &&
                x.Service.IsActive &&
                x.Service.Category.IsActive)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.DurationDays,
                x.RegionCode,
                x.CurrencyCode,
                x.BasePrice
            })
            .FirstOrDefaultAsync();

        if (planContext == null)
        {
            return null;
        }

        serviceContext.PlanId = planContext.Id;
        serviceContext.PlanName = planContext.Name;
        serviceContext.PlanDurationDays = planContext.DurationDays;
        serviceContext.PlanRegionCode = planContext.RegionCode;
        serviceContext.CurrencyCode = planContext.CurrencyCode;
        serviceContext.BasePrice = planContext.BasePrice;
        return serviceContext;
    }

    public async Task<decimal?> GetEffectiveExchangeRateAsync(
        string currencyCode,
        DateTime asOfUtc)
    {
        return await _context.ExchangeRates
            .AsNoTracking()
            .Where(x =>
                x.CurrencyCode == currencyCode &&
                x.IsActive &&
                x.EffectiveFrom <= asOfUtc)
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => (decimal?)x.RateToIrr)
            .FirstOrDefaultAsync();
    }

    public Task<decimal?> GetEffectiveServiceMarginAsync(
        Guid serviceId,
        DateTime asOfUtc)
    {
        return GetLatestMarginAsync(
            query => query.Where(x => x.ServiceId == serviceId),
            asOfUtc);
    }

    public Task<decimal?> GetEffectiveCategoryMarginAsync(
        Guid categoryId,
        DateTime asOfUtc)
    {
        return GetLatestMarginAsync(
            query => query.Where(x =>
                x.ServiceId == null &&
                x.CategoryId == categoryId),
            asOfUtc);
    }

    public Task<decimal?> GetEffectiveGlobalMarginAsync(DateTime asOfUtc)
    {
        return GetLatestMarginAsync(
            query => query.Where(x =>
                x.ServiceId == null &&
                x.CategoryId == null),
            asOfUtc);
    }

    private async Task<decimal?> GetLatestMarginAsync(
        Func<IQueryable<Domain.Entities.ProfitMargin>,
            IQueryable<Domain.Entities.ProfitMargin>> scope,
        DateTime asOfUtc)
    {
        var query = _context.ProfitMargins
            .AsNoTracking()
            .Where(x => x.IsActive && x.EffectiveFrom <= asOfUtc);

        return await scope(query)
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => (decimal?)x.Percentage)
            .FirstOrDefaultAsync();
    }
}
