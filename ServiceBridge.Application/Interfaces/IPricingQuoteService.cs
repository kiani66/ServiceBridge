using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IPricingQuoteService
{
    Task<ServiceResult<PricingQuoteResponse>> GetQuoteAsync(
        Guid serviceId,
        Guid? planId,
        DateTime quotedAtUtc);
}
