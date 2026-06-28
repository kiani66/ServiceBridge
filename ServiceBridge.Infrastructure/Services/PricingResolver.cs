using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class PricingResolver : IPricingResolver
{
    private readonly IPricingRepository _repository;

    public PricingResolver(IPricingRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<PricingResolutionResult>> ResolveAsync(
        Guid serviceId,
        DateTime asOfUtc)
    {
        if (asOfUtc.Kind != DateTimeKind.Utc)
        {
            return ServiceResult<PricingResolutionResult>.Fail(
                AppMessages.InvalidPricingDate,
                AppConstants.ErrorCodes.ValidationFailed,
                400);
        }

        var context = await _repository.GetActiveServiceContextAsync(serviceId);
        if (context == null)
        {
            return ServiceResult<PricingResolutionResult>.Fail(
                AppMessages.ServiceNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        return await ResolveAsync(context, asOfUtc);
    }

    public async Task<ServiceResult<PricingResolutionResult>> ResolveAsync(
        ServicePricingContext context,
        DateTime asOfUtc)
    {
        if (asOfUtc.Kind != DateTimeKind.Utc)
        {
            return ServiceResult<PricingResolutionResult>.Fail(
                AppMessages.InvalidPricingDate,
                AppConstants.ErrorCodes.ValidationFailed,
                400);
        }

        var exchangeRate = await _repository.GetEffectiveExchangeRateAsync(
            context.CurrencyCode,
            asOfUtc);

        if (!exchangeRate.HasValue)
        {
            return ServiceResult<PricingResolutionResult>.Fail(
                AppMessages.ExchangeRateNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var margin = await _repository.GetEffectiveServiceMarginAsync(
            context.ServiceId,
            asOfUtc);

        margin ??= await _repository.GetEffectiveCategoryMarginAsync(
            context.CategoryId,
            asOfUtc);

        margin ??= await _repository.GetEffectiveGlobalMarginAsync(asOfUtc);

        if (!margin.HasValue)
        {
            return ServiceResult<PricingResolutionResult>.Fail(
                AppMessages.ProfitMarginNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        return ServiceResult<PricingResolutionResult>.Ok(
            new PricingResolutionResult
            {
                ExchangeRate = exchangeRate.Value,
                MarginPercentage = margin.Value
            });
    }
}
