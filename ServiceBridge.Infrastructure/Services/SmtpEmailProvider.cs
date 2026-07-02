using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Infrastructure.Services;

public class SmtpEmailProvider : IMessageDeliveryProvider
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailProvider> _logger;

    public SmtpEmailProvider(
        IOptions<EmailOptions> options,
        ILogger<SmtpEmailProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ServiceResult> SendAsync(
        MessageOutbox message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (message.Channel != MessageChannel.Email)
        {
            return DeliveryFailure("SMTP provider only supports email messages.");
        }

        if (!_options.Enabled)
        {
            _logger.LogWarning(
                "Email delivery is disabled for outbox entry {MessageId}.",
                message.Id);

            return DeliveryFailure("Email delivery is disabled.");
        }

        using var timeoutCts = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            using var mailMessage = CreateMailMessage(message);
            using var client = CreateClient();

            await client.SendMailAsync(mailMessage, timeoutCts.Token);

            _logger.LogInformation(
                "SMTP email sent for outbox entry {MessageId}.",
                message.Id);

            return ServiceResult.Ok("Email delivered by SMTP provider.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogWarning(
                exception,
                "SMTP email delivery timed out for outbox entry {MessageId}.",
                message.Id);

            return DeliveryFailure("SMTP email delivery timed out.");
        }
        catch (SmtpException exception)
        {
            _logger.LogWarning(
                exception,
                "SMTP email delivery failed for outbox entry {MessageId}.",
                message.Id);

            return DeliveryFailure(GetSmtpFailureMessage(exception));
        }
        catch (Exception exception) when (
            exception is FormatException or InvalidOperationException)
        {
            _logger.LogWarning(
                exception,
                "SMTP email message could not be created for outbox entry {MessageId}.",
                message.Id);

            return DeliveryFailure("SMTP email message is invalid.");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "SMTP email delivery failed unexpectedly for outbox entry {MessageId}.",
                message.Id);

            return DeliveryFailure("SMTP email delivery failed.");
        }
    }

    private MailMessage CreateMailMessage(MessageOutbox message)
    {
        var from = string.IsNullOrWhiteSpace(_options.FromName)
            ? new MailAddress(_options.FromAddress.Trim())
            : new MailAddress(_options.FromAddress.Trim(), _options.FromName.Trim());

        var mailMessage = new MailMessage
        {
            From = from,
            Subject = message.Subject ?? string.Empty,
            Body = message.Body,
            IsBodyHtml = false
        };
        mailMessage.To.Add(message.Recipient.Trim());

        return mailMessage;
    }

    private SmtpClient CreateClient()
    {
        var client = new SmtpClient(_options.Host.Trim(), _options.Port)
        {
            EnableSsl = _options.UseSsl,
            Timeout = checked(_options.TimeoutSeconds * 1000)
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(
                _options.Username,
                _options.Password);
        }

        return client;
    }

    private static ServiceResult DeliveryFailure(string message)
    {
        return ServiceResult.Fail(
            message,
            AppConstants.ErrorCodes.Conflict,
            409);
    }

    private static string GetSmtpFailureMessage(SmtpException exception)
    {
        return exception.StatusCode == SmtpStatusCode.GeneralFailure
            ? "SMTP email delivery failed."
            : $"SMTP email delivery failed with status {exception.StatusCode}.";
    }
}
