using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeMessageOutboxRepository : IMessageOutboxRepository
{
    private readonly Dictionary<Guid, MessageOutbox> _messages = new();

    public int SaveChangesCallCount { get; private set; }
    public bool SaveChangesResult { get; set; } = true;
    public IReadOnlyCollection<MessageOutbox> Messages => _messages.Values.ToArray();

    public Task AddAsync(MessageOutbox message)
    {
        _messages[message.Id] = message;
        return Task.CompletedTask;
    }

    public Task<MessageOutbox?> GetForUpdateAsync(Guid id)
    {
        return Task.FromResult(_messages.GetValueOrDefault(id));
    }

    public Task<MessageOutboxDto?> GetByIdAsync(Guid id)
    {
        return Task.FromResult(
            _messages.TryGetValue(id, out var message)
                ? Map(message)
                : null);
    }

    public Task<bool> SaveChangesAsync()
    {
        SaveChangesCallCount++;
        return Task.FromResult(SaveChangesResult);
    }

    public void Seed(MessageOutbox message)
    {
        _messages[message.Id] = message;
    }

    private static MessageOutboxDto Map(MessageOutbox message)
    {
        return new MessageOutboxDto
        {
            Id = message.Id,
            UserId = message.UserId,
            Channel = message.Channel.ToString(),
            Recipient = message.Recipient,
            Subject = message.Subject,
            Body = message.Body,
            Status = message.Status.ToString(),
            RelatedEntityType = message.RelatedEntityType,
            RelatedEntityId = message.RelatedEntityId,
            Error = message.Error,
            AttemptCount = message.AttemptCount,
            CreatedAt = message.CreatedAt,
            NextAttemptAt = message.NextAttemptAt,
            ProcessedAt = message.ProcessedAt
        };
    }
}
