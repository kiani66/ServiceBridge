using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IMessageOutboxProcessor
{
    Task<ServiceResult<MessageOutboxProcessResult>> ProcessPendingAsync(
        CancellationToken cancellationToken = default);
}
