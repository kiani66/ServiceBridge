using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class AdminPricingService : IAdminPricingService
{
    private readonly IAdminPricingRepository _repository;

    public AdminPricingService(IAdminPricingRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<ExchangeRateResponse>> CreateExchangeRateAsync(
        CreateExchangeRateRequest request)
    {
        if (!IsUtc(request.EffectiveFromUtc))
        {
            return InvalidDate<ExchangeRateResponse>();
        }

        var exchangeRate = new ExchangeRate
        {
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            RateToIrr = request.RateToIrr,
            IsActive = request.IsActive,
            EffectiveFrom = request.EffectiveFromUtc
        };

        await _repository.AddExchangeRateAsync(exchangeRate);

        return ServiceResult<ExchangeRateResponse>.Created(
            PricingResponseMapper.MapExchangeRate(exchangeRate),
            AppMessages.ExchangeRateCreated);
    }

    public async Task<ServiceResult<ProfitMarginResponse>> CreateProfitMarginAsync(
        CreateProfitMarginRequest request)
    {
        if (!IsUtc(request.EffectiveFromUtc))
        {
            return InvalidDate<ProfitMarginResponse>();
        }

        var scopeCount = new[]
        {
            request.ServiceVariantId,
            request.ServiceId,
            request.CategoryId
        }.Count(x => x.HasValue);

        if (scopeCount > 1)
        {
            return ServiceResult<ProfitMarginResponse>.Fail(
                AppMessages.InvalidProfitMarginScope,
                AppConstants.ErrorCodes.ValidationFailed,
                400);
        }

        if (request.ServiceVariantId.HasValue &&
            !await _repository.ServiceVariantExistsAsync(request.ServiceVariantId.Value))
        {
            return ServiceResult<ProfitMarginResponse>.Fail(
                AppMessages.ServiceVariantNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (request.ServiceId.HasValue &&
            !await _repository.ServiceExistsAsync(request.ServiceId.Value))
        {
            return ServiceResult<ProfitMarginResponse>.Fail(
                AppMessages.ServiceNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (request.CategoryId.HasValue &&
            !await _repository.CategoryExistsAsync(request.CategoryId.Value))
        {
            return ServiceResult<ProfitMarginResponse>.Fail(
                AppMessages.CategoryNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var margin = new ProfitMargin
        {
            ServiceVariantId = request.ServiceVariantId,
            CategoryId = request.CategoryId,
            ServiceId = request.ServiceId,
            Percentage = request.Percentage,
            IsActive = request.IsActive,
            EffectiveFrom = request.EffectiveFromUtc
        };

        await _repository.AddProfitMarginAsync(margin);

        return ServiceResult<ProfitMarginResponse>.Created(
            PricingResponseMapper.MapProfitMargin(margin),
            AppMessages.ProfitMarginCreated);
    }

    public async Task<ServiceResult<ExchangeRateResponse>> UpdateExchangeRateStatusAsync(
        Guid id,
        UpdatePricingStatusRequest request)
    {
        var exchangeRate = await _repository.GetExchangeRateByIdAsync(id);
        if (exchangeRate == null)
        {
            return ServiceResult<ExchangeRateResponse>.Fail(
                AppMessages.ExchangeRateRecordNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        exchangeRate.IsActive = request.IsActive;
        exchangeRate.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync();

        return ServiceResult<ExchangeRateResponse>.Ok(
            PricingResponseMapper.MapExchangeRate(exchangeRate),
            AppMessages.ExchangeRateStatusUpdated);
    }

    public async Task<ServiceResult<ProfitMarginResponse>> UpdateProfitMarginStatusAsync(
        Guid id,
        UpdatePricingStatusRequest request)
    {
        var margin = await _repository.GetProfitMarginByIdAsync(id);
        if (margin == null)
        {
            return ServiceResult<ProfitMarginResponse>.Fail(
                AppMessages.ProfitMarginRecordNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        margin.IsActive = request.IsActive;
        margin.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync();

        return ServiceResult<ProfitMarginResponse>.Ok(
            PricingResponseMapper.MapProfitMargin(margin),
            AppMessages.ProfitMarginStatusUpdated);
    }

    private static bool IsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc;
    }

    private static ServiceResult<T> InvalidDate<T>()
    {
        return ServiceResult<T>.Fail(
            AppMessages.InvalidPricingDate,
            AppConstants.ErrorCodes.ValidationFailed,
            400);
    }
}
