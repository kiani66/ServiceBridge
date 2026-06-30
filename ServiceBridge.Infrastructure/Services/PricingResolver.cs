using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class PricingResolver : IPricingResolver
{
    private readonly IPricingRepository _repository;
    private readonly IPricingCalculator _calculator;

    public PricingResolver(
        IPricingRepository repository,
        IPricingCalculator calculator)
    {
        _repository = repository;
        _calculator = calculator;
    }

    public async Task<ServiceResult<PricingResolutionResult>> ResolveVariantAsync(
        Guid serviceVariantId,
        DateTime asOfUtc)
    {
        if (!IsUtc(asOfUtc))
        {
            return InvalidPricingDate();
        }

        var context = await _repository.GetVariantPricingContextAsync(serviceVariantId);
        if (context == null)
        {
            return ServiceResult<PricingResolutionResult>.Fail(
                AppMessages.ServiceVariantNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var availability = ValidateVariantAvailability(context);
        if (availability != null)
        {
            return availability;
        }

        return await ResolveAsync(context, asOfUtc);
    }

    public async Task<ServiceResult<PricingResolutionResult>> ResolveAsync(
        Guid serviceId,
        DateTime asOfUtc)
    {
        if (!IsUtc(asOfUtc))
        {
            return InvalidPricingDate();
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
        if (!IsUtc(asOfUtc))
        {
            return InvalidPricingDate();
        }

        var currencyCode = context.CurrencyCode.Trim().ToUpperInvariant();
        if (!CurrencyCodeExtensions.TryFromIsoCode(currencyCode, out _))
        {
            return ServiceResult<PricingResolutionResult>.Fail(
                "Unsupported currency code.",
                AppConstants.ErrorCodes.ValidationFailed,
                400);
        }

        var exchangeRate = await _repository.GetEffectiveExchangeRateAsync(
            currencyCode,
            asOfUtc);

        if (!exchangeRate.HasValue)
        {
            return ServiceResult<PricingResolutionResult>.Fail(
                AppMessages.ExchangeRateNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var margin = await ResolveMarginAsync(context, asOfUtc);

        if (!margin.Percentage.HasValue)
        {
            return ServiceResult<PricingResolutionResult>.Fail(
                AppMessages.ProfitMarginNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var calculation = _calculator.Calculate(
            context.BasePrice,
            exchangeRate.Value,
            margin.Percentage.Value);

        return ServiceResult<PricingResolutionResult>.Ok(
            new PricingResolutionResult
            {
                ServiceVariantId = context.ServiceVariantId,
                CurrencyCode = currencyCode,
                RegionCode = context.RegionCode ?? "GLOBAL",
                ForeignPrice = calculation.ForeignPrice,
                ExchangeRate = exchangeRate.Value,
                MarginPercent = calculation.MarginPercent,
                BaseAmountIrr = calculation.BaseAmountIrr,
                ProfitAmountIrr = calculation.ProfitAmountIrr,
                FinalAmountIrr = calculation.FinalAmountIrr,
                PricingSource = context.ServiceVariantId.HasValue
                    ? "ServiceVariant"
                    : "DigitalService",
                ExchangeRateSource = currencyCode,
                MarginSource = margin.Source
            });
    }

    private async Task<(decimal? Percentage, string Source)> ResolveMarginAsync(
        ServicePricingContext context,
        DateTime asOfUtc)
    {
        if (context.ServiceVariantId.HasValue)
        {
            var variantMargin = await _repository.GetEffectiveVariantMarginAsync(
                context.ServiceVariantId.Value,
                asOfUtc);
            if (variantMargin.HasValue)
            {
                return (variantMargin, "ServiceVariant");
            }
        }

        var serviceMargin = await _repository.GetEffectiveServiceMarginAsync(
            context.ServiceId,
            asOfUtc);
        if (serviceMargin.HasValue)
        {
            return (serviceMargin, "DigitalService");
        }

        var categoryMargin = await _repository.GetEffectiveCategoryMarginAsync(
            context.CategoryId,
            asOfUtc);
        if (categoryMargin.HasValue)
        {
            return (categoryMargin, "Category");
        }

        var globalMargin = await _repository.GetEffectiveGlobalMarginAsync(asOfUtc);
        return globalMargin.HasValue
            ? (globalMargin, "Global")
            : (null, string.Empty);
    }

    private static ServiceResult<PricingResolutionResult>? ValidateVariantAvailability(
        ServicePricingContext context)
    {
        if (!context.IsVariantActive)
        {
            return ServiceResult<PricingResolutionResult>.Fail(
                "The selected service variant is not active.",
                AppConstants.ErrorCodes.ValidationFailed,
                400);
        }

        if (!context.IsServiceActive)
        {
            return ServiceResult<PricingResolutionResult>.Fail(
                AppMessages.ServiceNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (!context.IsCategoryActive)
        {
            return ServiceResult<PricingResolutionResult>.Fail(
                AppMessages.CategoryNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (!context.IsProviderActive)
        {
            return ServiceResult<PricingResolutionResult>.Fail(
                AppMessages.ServiceNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        return null;
    }

    private static bool IsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc;
    }

    private static ServiceResult<PricingResolutionResult> InvalidPricingDate()
    {
        return ServiceResult<PricingResolutionResult>.Fail(
            AppMessages.InvalidPricingDate,
            AppConstants.ErrorCodes.ValidationFailed,
            400);
    }
}
