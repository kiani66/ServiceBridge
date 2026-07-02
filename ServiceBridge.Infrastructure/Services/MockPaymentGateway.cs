using System.Text.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Payments;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public sealed class MockPaymentGateway : IMockPaymentGateway, IPaymentGateway
{
    public string Provider => PaymentGatewayProviders.Mock;

    public Task<ServiceResult<PaymentGatewayInitiateResult>> InitiateAsync(
        PaymentGatewayInitiateRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ServiceResult<PaymentGatewayInitiateResult>.Ok(
            new PaymentGatewayInitiateResult
            {
                Provider = Provider,
                PaymentId = request.PaymentId,
                GatewayReference = $"MOCK-INIT-{request.PaymentId:N}"[..32],
                RequiresRedirect = false
            }));
    }

    public Task<ServiceResult<PaymentGatewayVerifyResult>> VerifyAsync(
        PaymentGatewayVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        var sanitizedPayload = PaymentGatewayPayloadSanitizer.Sanitize(request.RawPayload);

        try
        {
            var confirmRequest = JsonSerializer.Deserialize<ConfirmPaymentRequest>(
                request.RawPayload,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (confirmRequest == null || confirmRequest.PaymentId == Guid.Empty)
            {
                return Task.FromResult(InvalidVerification());
            }

            if (confirmRequest.Status == PaymentStatus.Pending)
            {
                return Task.FromResult(ServiceResult<PaymentGatewayVerifyResult>.Ok(
                    new PaymentGatewayVerifyResult
                    {
                        Provider = Provider,
                        IsVerified = false,
                        PaymentId = confirmRequest.PaymentId,
                        Status = PaymentStatus.Failed,
                        GatewayReference = confirmRequest.GatewayReference,
                        FailureReason = "Mock gateway callback cannot leave a payment pending.",
                        SanitizedPayload = sanitizedPayload
                    }));
            }

            return Task.FromResult(ServiceResult<PaymentGatewayVerifyResult>.Ok(
                new PaymentGatewayVerifyResult
                {
                    Provider = Provider,
                    IsVerified = true,
                    PaymentId = confirmRequest.PaymentId,
                    Status = confirmRequest.Status,
                    GatewayReference = confirmRequest.GatewayReference,
                    FailureReason = confirmRequest.FailureReason,
                    SanitizedPayload = sanitizedPayload
                }));
        }
        catch (JsonException)
        {
            return Task.FromResult(InvalidVerification());
        }

        ServiceResult<PaymentGatewayVerifyResult> InvalidVerification()
        {
            return ServiceResult<PaymentGatewayVerifyResult>.Fail(
                AppMessages.PaymentGatewayVerificationFailed,
                AppConstants.ErrorCodes.ValidationFailed,
                400);
        }
    }

    public PaymentGatewayCallback CreateCallback(Guid paymentId, bool succeed)
    {
        var processedAtUtc = DateTime.UtcNow;
        var gatewayReference = CreateGatewayReference(processedAtUtc, paymentId);
        var status = succeed ? PaymentStatus.Completed : PaymentStatus.Failed;
        var failureReason = succeed ? null : "Mock gateway declined the payment.";
        var request = new ConfirmPaymentRequest
        {
            PaymentId = paymentId,
            GatewayReference = gatewayReference,
            Status = status,
            FailureReason = failureReason
        };

        var payload = JsonSerializer.Serialize(
            new MockPaymentCallbackPayload(
                "MockGateway",
                paymentId,
                status.ToString(),
                gatewayReference,
                succeed,
                failureReason,
                processedAtUtc),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return new PaymentGatewayCallback(request, payload);
    }

    private static string CreateGatewayReference(DateTime processedAtUtc, Guid paymentId)
    {
        return $"MOCK-{processedAtUtc:yyyyMMddHHmmss}-{paymentId:N}"[..32];
    }

    private sealed record MockPaymentCallbackPayload(
        string Gateway,
        Guid PaymentId,
        string Status,
        string GatewayReference,
        bool Approved,
        string? FailureReason,
        DateTime ProcessedAtUtc);
}
