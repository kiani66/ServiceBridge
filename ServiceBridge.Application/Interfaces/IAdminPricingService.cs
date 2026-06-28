using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminPricingService
{
    Task<ServiceResult<ExchangeRateResponse>> CreateExchangeRateAsync(
        CreateExchangeRateRequest request);
    Task<ServiceResult<ProfitMarginResponse>> CreateProfitMarginAsync(
        CreateProfitMarginRequest request);
    Task<ServiceResult<ExchangeRateResponse>> UpdateExchangeRateStatusAsync(
        Guid id,
        UpdatePricingStatusRequest request);
    Task<ServiceResult<ProfitMarginResponse>> UpdateProfitMarginStatusAsync(
        Guid id,
        UpdatePricingStatusRequest request);
}
