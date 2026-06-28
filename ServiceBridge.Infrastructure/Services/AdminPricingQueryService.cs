using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Infrastructure.Services;

public class AdminPricingQueryService : IAdminPricingQueryService
{
    private readonly IAdminPricingRepository _repository;

    public AdminPricingQueryService(IAdminPricingRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<IReadOnlyCollection<ExchangeRateResponse>>> GetExchangeRatesAsync(
        string? currencyCode)
    {
        var normalizedCurrency = string.IsNullOrWhiteSpace(currencyCode)
            ? null
            : currencyCode.Trim().ToUpperInvariant();

        var exchangeRates = await _repository.GetExchangeRatesAsync(normalizedCurrency);
        var response = exchangeRates
            .Select(PricingResponseMapper.MapExchangeRate)
            .ToArray();

        return ServiceResult<IReadOnlyCollection<ExchangeRateResponse>>.Ok(response);
    }

    public async Task<ServiceResult<IReadOnlyCollection<ProfitMarginResponse>>> GetProfitMarginsAsync()
    {
        var margins = await _repository.GetProfitMarginsAsync();
        var response = margins
            .Select(PricingResponseMapper.MapProfitMargin)
            .ToArray();

        return ServiceResult<IReadOnlyCollection<ProfitMarginResponse>>.Ok(response);
    }
}
