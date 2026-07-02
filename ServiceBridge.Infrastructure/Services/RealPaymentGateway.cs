using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Payments;
using ServiceBridge.Application.Results;
using ServiceBridge.Application.Settings;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public sealed class RealPaymentGateway : IPaymentGateway
{
    private readonly PaymentGatewayOptions _options;
    private readonly ILogger<RealPaymentGateway> _logger;

    public RealPaymentGateway(
        IOptions<PaymentGatewayOptions> options,
        ILogger<RealPaymentGateway> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string Provider => PaymentGatewayProviders.Real;

    public Task<ServiceResult<PaymentGatewayInitiateResult>> InitiateAsync(
        PaymentGatewayInitiateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Real.Enabled)
        {
            _logger.LogInformation(
                "Real payment gateway initiation rejected because the provider is disabled.");

            return Task.FromResult(ServiceResult<PaymentGatewayInitiateResult>.Fail(
                AppMessages.PaymentGatewayDisabled,
                AppConstants.ErrorCodes.PaymentGatewayUnavailable,
                503));
        }

        _logger.LogWarning(
            "Real payment gateway initiation requested before a provider adapter is configured.");

        return Task.FromResult(ServiceResult<PaymentGatewayInitiateResult>.Fail(
            AppMessages.PaymentGatewayUnavailable,
            AppConstants.ErrorCodes.PaymentGatewayUnavailable,
            503));
    }

    public Task<ServiceResult<PaymentGatewayVerifyResult>> VerifyAsync(
        PaymentGatewayVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        var sanitizedPayload = PaymentGatewayPayloadSanitizer.Sanitize(request.RawPayload);

        if (TryReadPaymentId(request.RawPayload, out var paymentId))
        {
            return Task.FromResult(ServiceResult<PaymentGatewayVerifyResult>.Ok(
                new PaymentGatewayVerifyResult
                {
                    Provider = Provider,
                    IsVerified = false,
                    PaymentId = paymentId,
                    Status = PaymentStatus.Failed,
                    FailureReason = _options.Real.Enabled
                        ? AppMessages.PaymentGatewayVerificationFailed
                        : AppMessages.PaymentGatewayDisabled,
                    SanitizedPayload = sanitizedPayload
                }));
        }

        return Task.FromResult(ServiceResult<PaymentGatewayVerifyResult>.Fail(
            _options.Real.Enabled
                ? AppMessages.PaymentGatewayVerificationFailed
                : AppMessages.PaymentGatewayDisabled,
            AppConstants.ErrorCodes.PaymentGatewayUnavailable,
            503));
    }

    private static bool TryReadPaymentId(string payload, out Guid paymentId)
    {
        paymentId = Guid.Empty;

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("paymentId", out var paymentIdElement))
            {
                return false;
            }

            return paymentIdElement.ValueKind == System.Text.Json.JsonValueKind.String &&
                   Guid.TryParse(paymentIdElement.GetString(), out paymentId);
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
