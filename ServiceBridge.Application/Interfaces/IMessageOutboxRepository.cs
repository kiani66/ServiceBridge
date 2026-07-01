using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IMessageOutboxRepository
{
    Task AddAsync(MessageOutbox message);
    Task<MessageOutbox?> GetForUpdateAsync(Guid id);
    Task<IReadOnlyList<MessageOutbox>> GetDuePendingAsync(
        DateTime dueAtUtc,
        int batchSize,
        CancellationToken cancellationToken = default);
    Task<MessageOutboxDto?> GetByIdAsync(Guid id);
    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}
