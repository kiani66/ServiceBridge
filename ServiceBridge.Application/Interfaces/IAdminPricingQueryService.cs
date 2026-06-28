using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminPricingQueryService
{
    Task<ServiceResult<IReadOnlyCollection<ExchangeRateResponse>>> GetExchangeRatesAsync(
        string? currencyCode);
    Task<ServiceResult<IReadOnlyCollection<ProfitMarginResponse>>> GetProfitMarginsAsync();
}
