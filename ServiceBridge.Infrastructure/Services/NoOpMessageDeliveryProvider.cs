using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Infrastructure.Services;

public class NoOpMessageDeliveryProvider : IMessageDeliveryProvider
{
    public Task<ServiceResult> SendAsync(
        MessageOutbox message,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ServiceResult.Ok("Message accepted by no-op provider."));
    }
}
