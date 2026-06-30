using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeAdminPricingRepository : IAdminPricingRepository
{
    private readonly Dictionary<Guid, ExchangeRate> _exchangeRates = new();
    private readonly Dictionary<Guid, ProfitMargin> _profitMargins = new();
    private readonly HashSet<Guid> _serviceVariantIds = new();
    private readonly HashSet<Guid> _categoryIds = new();
    private readonly HashSet<Guid> _serviceIds = new();

    public int SaveChangesCallCount { get; private set; }

    public void SeedCategory(Guid id)
    {
        _categoryIds.Add(id);
    }

    public void SeedService(Guid id)
    {
        _serviceIds.Add(id);
    }

    public void SeedServiceVariant(Guid id)
    {
        _serviceVariantIds.Add(id);
    }

    public void SeedExchangeRate(ExchangeRate exchangeRate)
    {
        _exchangeRates[exchangeRate.Id] = exchangeRate;
    }

    public void SeedProfitMargin(ProfitMargin profitMargin)
    {
        _profitMargins[profitMargin.Id] = profitMargin;
    }

    public Task<IReadOnlyCollection<ExchangeRate>> GetExchangeRatesAsync(string? currencyCode)
    {
        IReadOnlyCollection<ExchangeRate> result = _exchangeRates.Values
            .Where(x => currencyCode == null || x.CurrencyCode == currencyCode)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<ProfitMargin>> GetProfitMarginsAsync()
    {
        IReadOnlyCollection<ProfitMargin> result = _profitMargins.Values.ToArray();
        return Task.FromResult(result);
    }

    public Task<ExchangeRate?> GetExchangeRateByIdAsync(Guid id)
    {
        return Task.FromResult(_exchangeRates.GetValueOrDefault(id));
    }

    public Task<ProfitMargin?> GetProfitMarginByIdAsync(Guid id)
    {
        return Task.FromResult(_profitMargins.GetValueOrDefault(id));
    }

    public Task<bool> ServiceVariantExistsAsync(Guid id)
    {
        return Task.FromResult(_serviceVariantIds.Contains(id));
    }

    public Task<bool> CategoryExistsAsync(Guid id)
    {
        return Task.FromResult(_categoryIds.Contains(id));
    }

    public Task<bool> ServiceExistsAsync(Guid id)
    {
        return Task.FromResult(_serviceIds.Contains(id));
    }

    public Task AddExchangeRateAsync(ExchangeRate exchangeRate)
    {
        _exchangeRates[exchangeRate.Id] = exchangeRate;
        return Task.CompletedTask;
    }

    public Task AddProfitMarginAsync(ProfitMargin profitMargin)
    {
        _profitMargins[profitMargin.Id] = profitMargin;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync()
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
