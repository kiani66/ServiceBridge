using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class SmtpEmailProviderTests
{
    [Fact]
    public async Task SendAsync_WhenEmailDisabled_ReturnsFailure()
    {
        var provider = CreateProvider(new EmailOptions { Enabled = false });
        var message = CreateMessage(MessageChannel.Email);

        var result = await provider.SendAsync(message);

        Assert.False(result.Success);
        Assert.Equal("Email delivery is disabled.", result.Message);
    }

    [Fact]
    public async Task SendAsync_WhenChannelIsNotEmail_ReturnsFailure()
    {
        var provider = CreateProvider(new EmailOptions { Enabled = false });
        var message = CreateMessage(MessageChannel.Sms);

        var result = await provider.SendAsync(message);

        Assert.False(result.Success);
        Assert.Equal("SMTP provider only supports email messages.", result.Message);
    }

    [Fact]
    public async Task SendAsync_WhenRecipientInvalid_ReturnsFailureBeforeSmtpConnection()
    {
        var provider = CreateProvider(new EmailOptions
        {
            Enabled = true,
            Host = "smtp.example.com",
            FromAddress = "notifications@example.com"
        });
        var message = MessageOutbox.Create(
            MessageChannel.Email,
            "not an email address",
            "Subject",
            "Body",
            null,
            null,
            null,
            DateTime.UtcNow);

        var result = await provider.SendAsync(message);

        Assert.False(result.Success);
        Assert.Equal("SMTP email message is invalid.", result.Message);
    }

    private static SmtpEmailProvider CreateProvider(EmailOptions options)
    {
        return new SmtpEmailProvider(
            Options.Create(options),
            NullLogger<SmtpEmailProvider>.Instance);
    }

    private static MessageOutbox CreateMessage(MessageChannel channel)
    {
        return MessageOutbox.Create(
            channel,
            "user@example.com",
            "Subject",
            "Body",
            null,
            null,
            null,
            DateTime.UtcNow);
    }
}
