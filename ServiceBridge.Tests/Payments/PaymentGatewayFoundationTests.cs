using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ServiceBridge.Application.Payments;
using ServiceBridge.Application.Settings;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Payments;

public class PaymentGatewayFoundationTests
{
    [Fact]
    public void OptionsValidator_WhenRealGatewayDisabled_DoesNotRequireSecrets()
    {
        var validator = new PaymentGatewayOptionsValidator();

        var result = validator.Validate(
            null,
            new PaymentGatewayOptions
            {
                DefaultProvider = PaymentGatewayProviders.Mock,
                Real = new RealPaymentGatewayOptions
                {
                    Enabled = false
                }
            });

        Assert.False(result.Failed);
    }

    [Fact]
    public void OptionsValidator_WhenRealGatewayEnabled_RequiresProductionSettings()
    {
        var validator = new PaymentGatewayOptionsValidator();

        var result = validator.Validate(
            null,
            new PaymentGatewayOptions
            {
                DefaultProvider = PaymentGatewayProviders.Real,
                Real = new RealPaymentGatewayOptions
                {
                    Enabled = true
                }
            });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("MerchantId"));
        Assert.Contains(result.Failures, failure => failure.Contains("ApiKey"));
        Assert.Contains(result.Failures, failure => failure.Contains("CallbackBaseUrl"));
    }

    [Fact]
    public async Task RealGateway_WhenDisabled_ReturnsControlledInitiationFailure()
    {
        var gateway = new RealPaymentGateway(
            Options.Create(new PaymentGatewayOptions
            {
                DefaultProvider = PaymentGatewayProviders.Mock,
                Real = new RealPaymentGatewayOptions
                {
                    Enabled = false
                }
            }),
            NullLogger<RealPaymentGateway>.Instance);

        var result = await gateway.InitiateAsync(
            new PaymentGatewayInitiateRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                1_000_000m,
                10m,
                100_000m));

        Assert.False(result.Success);
        Assert.Equal(503, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.PaymentGatewayUnavailable, result.ErrorCode);
    }

    [Fact]
    public void Factory_WhenProviderIsUnsupported_ReturnsValidationFailure()
    {
        var factory = new PaymentGatewayFactory(
            [new FakePaymentGateway()],
            Options.Create(new PaymentGatewayOptions
            {
                DefaultProvider = PaymentGatewayProviders.Mock
            }),
            NullLogger<PaymentGatewayFactory>.Instance);

        var result = factory.GetGateway("UnknownProvider");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.ValidationFailed, result.ErrorCode);
    }

    [Fact]
    public void PayloadSanitizer_RedactsSensitiveJsonFields()
    {
        var sanitized = PaymentGatewayPayloadSanitizer.Sanitize(
            """{"paymentId":"p-1","apiKey":"secret-value","nested":{"signature":"signature-value","amount":100}}""");

        Assert.Contains("[REDACTED]", sanitized);
        Assert.Contains("p-1", sanitized);
        Assert.Contains("100", sanitized);
        Assert.DoesNotContain("secret-value", sanitized);
        Assert.DoesNotContain("signature-value", sanitized);
    }
}
