using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class MessageOutboxRepository : IMessageOutboxRepository
{
    private readonly ServiceBridgeDbContext _context;

    public MessageOutboxRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(MessageOutbox message)
    {
        await _context.MessageOutbox.AddAsync(message);
    }

    public Task<MessageOutbox?> GetForUpdateAsync(Guid id)
    {
        return _context.MessageOutbox.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<MessageOutboxDto?> GetByIdAsync(Guid id)
    {
        return _context.MessageOutbox
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new MessageOutboxDto
            {
                Id = x.Id,
                UserId = x.UserId,
                Channel = x.Channel.ToString(),
                Recipient = x.Recipient,
                Subject = x.Subject,
                Body = x.Body,
                Status = x.Status.ToString(),
                RelatedEntityType = x.RelatedEntityType,
                RelatedEntityId = x.RelatedEntityId,
                Error = x.Error,
                AttemptCount = x.AttemptCount,
                CreatedAt = x.CreatedAt,
                NextAttemptAt = x.NextAttemptAt,
                ProcessedAt = x.ProcessedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return false;
        }
    }
}
