using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IPricingResolver
{
    Task<ServiceResult<PricingResolutionResult>> ResolveVariantAsync(
        Guid serviceVariantId,
        DateTime asOfUtc);

    Task<ServiceResult<PricingResolutionResult>> ResolveAsync(
        Guid serviceId,
        DateTime asOfUtc);

    Task<ServiceResult<PricingResolutionResult>> ResolveAsync(
        ServicePricingContext context,
        DateTime asOfUtc);
}
