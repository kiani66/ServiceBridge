using Microsoft.Extensions.Options;
using ServiceBridge.Infrastructure.Services;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class EmailOptionsValidatorTests
{
    [Fact]
    public void Validate_WhenDisabled_AllowsEmptySmtpSettings()
    {
        var validator = new EmailOptionsValidator();

        var result = validator.Validate(null, new EmailOptions());

        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_WhenEnabledAndHostMissing_Fails()
    {
        var validator = new EmailOptionsValidator();

        var result = validator.Validate(null, new EmailOptions
        {
            Enabled = true,
            FromAddress = "notifications@example.com"
        });

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_WhenEnabledAndFromAddressInvalid_Fails()
    {
        var validator = new EmailOptionsValidator();

        var result = validator.Validate(null, new EmailOptions
        {
            Enabled = true,
            Host = "smtp.example.com",
            FromAddress = "not an email address"
        });

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_WhenEnabledAndConfigured_Succeeds()
    {
        var validator = new EmailOptionsValidator();

        var result = validator.Validate(null, new EmailOptions
        {
            Enabled = true,
            Host = "smtp.example.com",
            Port = 587,
            UseSsl = true,
            FromAddress = "notifications@example.com",
            TimeoutSeconds = 30
        });

        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_WhenOnlyUsernameConfigured_Fails()
    {
        var validator = new EmailOptionsValidator();

        var result = validator.Validate(null, new EmailOptions
        {
            Username = "smtp-user"
        });

        Assert.True(result.Failed);
    }
}
