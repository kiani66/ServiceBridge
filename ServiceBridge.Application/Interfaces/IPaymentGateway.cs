using ServiceBridge.Application.Payments;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IPaymentGateway
{
    string Provider { get; }

    Task<ServiceResult<PaymentGatewayInitiateResult>> InitiateAsync(
        PaymentGatewayInitiateRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PaymentGatewayVerifyResult>> VerifyAsync(
        PaymentGatewayVerifyRequest request,
        CancellationToken cancellationToken = default);
}
