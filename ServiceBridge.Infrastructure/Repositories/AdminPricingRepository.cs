using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class AdminPricingRepository : IAdminPricingRepository
{
    private readonly ServiceBridgeDbContext _context;

    public AdminPricingRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ExchangeRate>> GetExchangeRatesAsync(
        string? currencyCode)
    {
        var query = _context.ExchangeRates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(currencyCode))
        {
            query = query.Where(x => x.CurrencyCode == currencyCode);
        }

        return await query
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<ProfitMargin>> GetProfitMarginsAsync()
    {
        return await _context.ProfitMargins
            .AsNoTracking()
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<ExchangeRate?> GetExchangeRateByIdAsync(Guid id)
    {
        return await _context.ExchangeRates.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<ProfitMargin?> GetProfitMarginByIdAsync(Guid id)
    {
        return await _context.ProfitMargins.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<bool> CategoryExistsAsync(Guid id)
    {
        return _context.ServiceCategories.AnyAsync(x => x.Id == id);
    }

    public Task<bool> ServiceVariantExistsAsync(Guid id)
    {
        return _context.ServiceVariants.AnyAsync(x => x.Id == id);
    }

    public Task<bool> ServiceExistsAsync(Guid id)
    {
        return _context.DigitalServices.AnyAsync(x => x.Id == id);
    }

    public async Task AddExchangeRateAsync(ExchangeRate exchangeRate)
    {
        await _context.ExchangeRates.AddAsync(exchangeRate);
        await _context.SaveChangesAsync();
    }

    public async Task AddProfitMarginAsync(ProfitMargin profitMargin)
    {
        await _context.ProfitMargins.AddAsync(profitMargin);
        await _context.SaveChangesAsync();
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
