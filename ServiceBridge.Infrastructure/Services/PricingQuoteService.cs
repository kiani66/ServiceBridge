using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class PricingQuoteService : IPricingQuoteService
{
    private readonly IPricingRepository _repository;
    private readonly IPricingResolver _resolver;
    private readonly IPricingCalculator _calculator;

    public PricingQuoteService(
        IPricingRepository repository,
        IPricingResolver resolver,
        IPricingCalculator calculator)
    {
        _repository = repository;
        _resolver = resolver;
        _calculator = calculator;
    }

    public async Task<ServiceResult<PricingQuoteResponse>> GetQuoteAsync(
        Guid serviceId,
        Guid? serviceVariantId,
        DateTime quotedAtUtc)
    {
        if (quotedAtUtc.Kind != DateTimeKind.Utc)
        {
            return ServiceResult<PricingQuoteResponse>.Fail(
                AppMessages.InvalidPricingDate,
                AppConstants.ErrorCodes.ValidationFailed,
                400);
        }

        var context = await _repository.GetActiveServiceContextAsync(
            serviceId,
            serviceVariantId);
        if (context == null)
        {
            return ServiceResult<PricingQuoteResponse>.Fail(
                serviceVariantId.HasValue
                    ? "The selected service variant was not found."
                    : AppMessages.ServiceNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var resolution = await _resolver.ResolveAsync(context, quotedAtUtc);
        if (!resolution.Success || resolution.Data == null)
        {
            return ServiceResult<PricingQuoteResponse>.Fail(
                resolution.Message ?? AppMessages.PricingUnavailable,
                resolution.ErrorCode ?? AppConstants.ErrorCodes.NotFound,
                resolution.StatusCode);
        }

        var calculation = _calculator.Calculate(
            context.BasePrice,
            resolution.Data.ExchangeRate,
            resolution.Data.MarginPercentage);

        return ServiceResult<PricingQuoteResponse>.Ok(
            new PricingQuoteResponse
            {
                ServiceId = context.ServiceId,
                ServiceVariantId = context.ServiceVariantId,
                PlanId = context.PlanId,
                PlanName = context.PlanName,
                PlanDurationDays = context.PlanDurationDays,
                PlanRegionCode = context.PlanRegionCode,
                CurrencyCode = context.CurrencyCode,
                ForeignPrice = calculation.ForeignPrice,
                ExchangeRate = calculation.ExchangeRate,
                MarginPercentage = calculation.MarginPercent,
                BaseAmountIrr = calculation.BaseAmountIrr,
                ProfitAmountIrr = calculation.ProfitAmountIrr,
                FinalAmountIrr = calculation.FinalAmountIrr,
                QuotedAtUtc = quotedAtUtc
            });
    }
}
