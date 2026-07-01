using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IMessageDeliveryProvider
{
    Task<ServiceResult> SendAsync(
        MessageOutbox message,
        CancellationToken cancellationToken = default);
}
