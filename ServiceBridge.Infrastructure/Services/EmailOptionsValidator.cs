using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace ServiceBridge.Infrastructure.Services;

public class EmailOptionsValidator : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        var failures = new List<string>();

        if (options.Port is < 1 or > 65535)
        {
            failures.Add("Email:Port must be between 1 and 65535.");
        }

        if (options.TimeoutSeconds is < 1 or > 300)
        {
            failures.Add("Email:TimeoutSeconds must be between 1 and 300.");
        }

        var hasUsername = !string.IsNullOrWhiteSpace(options.Username);
        var hasPassword = !string.IsNullOrWhiteSpace(options.Password);
        if (hasUsername != hasPassword)
        {
            failures.Add("Email:Username and Email:Password must be configured together.");
        }

        if (options.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.Host))
            {
                failures.Add("Email:Host is required when email delivery is enabled.");
            }

            if (string.IsNullOrWhiteSpace(options.FromAddress))
            {
                failures.Add("Email:FromAddress is required when email delivery is enabled.");
            }
            else if (!IsValidEmailAddress(options.FromAddress))
            {
                failures.Add("Email:FromAddress must be a valid email address.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsValidEmailAddress(string value)
    {
        try
        {
            _ = new MailAddress(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
