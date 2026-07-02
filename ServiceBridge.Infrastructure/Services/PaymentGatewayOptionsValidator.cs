using Microsoft.Extensions.Options;
using ServiceBridge.Application.Payments;
using ServiceBridge.Application.Settings;

namespace ServiceBridge.Infrastructure.Services;

public sealed class PaymentGatewayOptionsValidator :
    IValidateOptions<PaymentGatewayOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        PaymentGatewayOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.DefaultProvider))
        {
            failures.Add("PaymentGateway:DefaultProvider must be configured.");
        }
        else if (!IsSupportedProvider(options.DefaultProvider))
        {
            failures.Add("PaymentGateway:DefaultProvider is not supported.");
        }

        if (!options.Real.Enabled)
        {
            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }

        if (string.IsNullOrWhiteSpace(options.Real.MerchantId))
        {
            failures.Add("PaymentGateway:Real:MerchantId must be configured when the real gateway is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.Real.ApiKey))
        {
            failures.Add("PaymentGateway:Real:ApiKey must be configured when the real gateway is enabled.");
        }

        if (!Uri.TryCreate(
                options.Real.CallbackBaseUrl,
                UriKind.Absolute,
                out var callbackBaseUrl) ||
            callbackBaseUrl.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add("PaymentGateway:Real:CallbackBaseUrl must be an absolute HTTPS URL when the real gateway is enabled.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsSupportedProvider(string provider)
    {
        return string.Equals(
                   provider,
                   PaymentGatewayProviders.Mock,
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   provider,
                   PaymentGatewayProviders.Real,
                   StringComparison.OrdinalIgnoreCase);
    }
}
