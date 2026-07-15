using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Payments;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakePaymentGateway : IPaymentGateway
{
    public string Provider { get; init; } = PaymentGatewayProviders.Mock;
    public ServiceResult<PaymentGatewayInitiateResult>? InitiateResult { get; set; }
    public PaymentGatewayVerifyResult? VerifyResult { get; set; }
    public int InitiateCallCount { get; private set; }
    public int VerifyCallCount { get; private set; }
    public PaymentGatewayInitiateRequest? LastInitiateRequest { get; private set; }
    public IList<string>? Operations { get; set; }

    public Task<ServiceResult<PaymentGatewayInitiateResult>> InitiateAsync(
        PaymentGatewayInitiateRequest request,
        CancellationToken cancellationToken = default)
    {
        InitiateCallCount++;
        LastInitiateRequest = request;

        return Task.FromResult(InitiateResult ??
            ServiceResult<PaymentGatewayInitiateResult>.Ok(
                new PaymentGatewayInitiateResult
                {
                    Provider = Provider,
                    PaymentId = request.PaymentId,
                    GatewayReference = $"{Provider}-init"
                }));
    }

    public Task<ServiceResult<PaymentGatewayVerifyResult>> VerifyAsync(
        PaymentGatewayVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        VerifyCallCount++;
        Operations?.Add("gateway.verify");

        return Task.FromResult(ServiceResult<PaymentGatewayVerifyResult>.Ok(
            VerifyResult ??
            new PaymentGatewayVerifyResult
            {
                Provider = Provider,
                IsVerified = true,
                Status = PaymentStatus.Completed,
                SanitizedPayload = PaymentGatewayPayloadSanitizer.Sanitize(request.RawPayload)
            }));
    }
}
