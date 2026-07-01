using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeMessageOutboxRepository : IMessageOutboxRepository
{
    private readonly Dictionary<Guid, MessageOutbox> _messages = new();
    private readonly List<MessageOutbox> _dueMessagesOverride = new();

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

    public Task<IReadOnlyList<MessageOutbox>> GetDuePendingAsync(
        DateTime dueAtUtc,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (_dueMessagesOverride.Count > 0)
        {
            return Task.FromResult<IReadOnlyList<MessageOutbox>>(
                _dueMessagesOverride.Take(batchSize).ToArray());
        }

        var messages = _messages.Values
            .Where(x => x.Status == MessageStatus.Pending)
            .Where(x => x.NextAttemptAt == null || x.NextAttemptAt <= dueAtUtc)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Take(batchSize)
            .ToArray();

        return Task.FromResult<IReadOnlyList<MessageOutbox>>(messages);
    }

    public Task<MessageOutboxDto?> GetByIdAsync(Guid id)
    {
        return Task.FromResult(
            _messages.TryGetValue(id, out var message)
                ? Map(message)
                : null);
    }

    public Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(SaveChangesResult);
    }

    public void Seed(MessageOutbox message)
    {
        _messages[message.Id] = message;
    }

    public void SeedDueOverride(params MessageOutbox[] messages)
    {
        _dueMessagesOverride.AddRange(messages);
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
