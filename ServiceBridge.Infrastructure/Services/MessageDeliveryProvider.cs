using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Infrastructure.Services;

public class MessageDeliveryProvider : IMessageDeliveryProvider
{
    private readonly SmtpEmailProvider _emailProvider;
    private readonly ISmsProvider _smsProvider;

    public MessageDeliveryProvider(
        SmtpEmailProvider emailProvider,
        ISmsProvider smsProvider)
    {
        _emailProvider = emailProvider;
        _smsProvider = smsProvider;
    }

    public Task<ServiceResult> SendAsync(
        MessageOutbox message,
        CancellationToken cancellationToken = default)
    {
        return message.Channel switch
        {
            MessageChannel.Email => _emailProvider.SendAsync(message, cancellationToken),
            MessageChannel.Sms => _smsProvider.SendAsync(message, cancellationToken),
            _ => Task.FromResult(ServiceResult.Fail(
                "Message channel is not supported.",
                AppConstants.ErrorCodes.Conflict,
                409))
        };
    }
}
