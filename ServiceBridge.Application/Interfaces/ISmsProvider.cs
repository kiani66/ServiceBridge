using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface ISmsProvider
{
    Task<ServiceResult> SendAsync(
        MessageOutbox message,
        CancellationToken cancellationToken = default);
}
