using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IPaymentService
{
    Task<ServiceResult<PaymentInitiatedResponse>> InitiateAsync(
        Guid userId,
        InitiatePaymentRequest request);

    Task<ServiceResult<MockPaymentResultResponse>> ProcessMockAsync(
        Guid userId,
        MockPaymentRequest request);

    Task<ServiceResult> ConfirmAsync(
        ConfirmPaymentRequest request,
        string callbackPayload);
}
