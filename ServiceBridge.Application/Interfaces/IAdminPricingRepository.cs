using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminPricingRepository
{
    Task<IReadOnlyCollection<ExchangeRate>> GetExchangeRatesAsync(string? currencyCode);
    Task<IReadOnlyCollection<ProfitMargin>> GetProfitMarginsAsync();
    Task<ExchangeRate?> GetExchangeRateByIdAsync(Guid id);
    Task<ProfitMargin?> GetProfitMarginByIdAsync(Guid id);
    Task<bool> ServiceVariantExistsAsync(Guid id);
    Task<bool> CategoryExistsAsync(Guid id);
    Task<bool> ServiceExistsAsync(Guid id);
    Task AddExchangeRateAsync(ExchangeRate exchangeRate);
    Task AddProfitMarginAsync(ProfitMargin profitMargin);
    Task SaveChangesAsync();
}
