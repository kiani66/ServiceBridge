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

    public PricingQuoteService(
        IPricingRepository repository,
        IPricingResolver resolver)
    {
        _repository = repository;
        _resolver = resolver;
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

        return ServiceResult<PricingQuoteResponse>.Ok(
            new PricingQuoteResponse
            {
                ServiceId = context.ServiceId,
                ServiceVariantId = resolution.Data.ServiceVariantId,
                PlanId = resolution.Data.ServiceVariantId,
                PlanName = context.PlanName,
                PlanDurationDays = context.PlanDurationDays,
                PlanRegionCode = context.PlanRegionCode,
                CurrencyCode = resolution.Data.CurrencyCode,
                ForeignPrice = resolution.Data.ForeignPrice,
                ExchangeRate = resolution.Data.ExchangeRate,
                MarginPercentage = resolution.Data.MarginPercent,
                BaseAmountIrr = resolution.Data.BaseAmountIrr,
                ProfitAmountIrr = resolution.Data.ProfitAmountIrr,
                FinalAmountIrr = resolution.Data.FinalAmountIrr,
                QuotedAtUtc = quotedAtUtc
            });
    }
}
