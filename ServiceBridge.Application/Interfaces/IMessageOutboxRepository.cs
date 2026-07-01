using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IMessageOutboxRepository
{
    Task AddAsync(MessageOutbox message);
    Task<MessageOutbox?> GetForUpdateAsync(Guid id);
    Task<MessageOutboxDto?> GetByIdAsync(Guid id);
    Task<bool> SaveChangesAsync();
}
