using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IMessageOutboxService
{
    Task<ServiceResult<MessageOutboxDto>> EnqueueAsync(EnqueueMessageRequest request);
    Task<ServiceResult<MessageOutboxDto>> MarkProcessingAsync(Guid id);
    Task<ServiceResult<MessageOutboxDto>> MarkSentAsync(Guid id);
    Task<ServiceResult<MessageOutboxDto>> MarkFailedAsync(
        Guid id,
        string error,
        DateTime? nextAttemptAtUtc);
    Task<ServiceResult<MessageOutboxDto>> CancelAsync(Guid id, string? reason);
}
