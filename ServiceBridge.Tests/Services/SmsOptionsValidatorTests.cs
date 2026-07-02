using ServiceBridge.Infrastructure.Services;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class SmsOptionsValidatorTests
{
    [Fact]
    public void Validate_WhenDisabled_AllowsEmptySettings()
    {
        var validator = new SmsOptionsValidator();

        var result = validator.Validate(null, new SmsOptions
        {
            Enabled = false,
            BaseUrl = "",
            ApiKey = "",
            Sender = ""
        });

        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_WhenEnabledAndRequiredSettingsMissing_Fails()
    {
        var validator = new SmsOptionsValidator();

        var result = validator.Validate(null, new SmsOptions
        {
            Enabled = true,
            Provider = "Http"
        });

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_WhenEnabledAndConfigured_Succeeds()
    {
        var validator = new SmsOptionsValidator();

        var result = validator.Validate(null, new SmsOptions
        {
            Enabled = true,
            Provider = "Http",
            BaseUrl = "https://sms.example.local/send",
            ApiKey = "test-api-key",
            Sender = "ServiceBridge",
            TimeoutSeconds = 30
        });

        Assert.False(result.Failed);
    }
}
