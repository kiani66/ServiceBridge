using Microsoft.Extensions.Options;

namespace ServiceBridge.Infrastructure.Services;

public class SmsOptionsValidator : IValidateOptions<SmsOptions>
{
    private static readonly StringComparer ProviderComparer =
        StringComparer.OrdinalIgnoreCase;

    public ValidateOptionsResult Validate(string? name, SmsOptions options)
    {
        var failures = new List<string>();

        if (options.TimeoutSeconds is < 1 or > 300)
        {
            failures.Add("Sms:TimeoutSeconds must be between 1 and 300.");
        }

        if (options.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.Provider))
            {
                failures.Add("Sms:Provider is required when SMS delivery is enabled.");
            }
            else if (!ProviderComparer.Equals(options.Provider.Trim(), "Http"))
            {
                failures.Add("Sms:Provider must be 'Http' for the configured SMS adapter.");
            }

            if (string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                failures.Add("Sms:BaseUrl is required when SMS delivery is enabled.");
            }
            else if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme is not ("http" or "https"))
            {
                failures.Add("Sms:BaseUrl must be an absolute HTTP or HTTPS URL.");
            }

            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                failures.Add("Sms:ApiKey is required when SMS delivery is enabled.");
            }

            if (string.IsNullOrWhiteSpace(options.Sender))
            {
                failures.Add("Sms:Sender is required when SMS delivery is enabled.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
