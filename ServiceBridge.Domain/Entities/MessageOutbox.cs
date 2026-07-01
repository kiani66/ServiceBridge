using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Entities;

public class MessageOutbox : BaseEntity
{
    public Guid? UserId { get; private set; }
    public MessageChannel Channel { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public string? Subject { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public MessageStatus Status { get; private set; } = MessageStatus.Pending;
    public string? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string? Error { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    public User? User { get; private set; }

    public static MessageOutbox Create(
        MessageChannel channel,
        string recipient,
        string? subject,
        string body,
        Guid? userId,
        string? relatedEntityType,
        Guid? relatedEntityId,
        DateTime createdAtUtc)
    {
        return new MessageOutbox
        {
            Id = Guid.NewGuid(),
            Channel = channel,
            Recipient = recipient.Trim(),
            Subject = NormalizeOptional(subject),
            Body = body,
            UserId = userId,
            RelatedEntityType = NormalizeOptional(relatedEntityType),
            RelatedEntityId = relatedEntityId,
            CreatedAt = createdAtUtc,
            Status = MessageStatus.Pending
        };
    }

    public void MarkProcessing(DateTime processingAtUtc)
    {
        if (Status is not (MessageStatus.Pending or MessageStatus.Failed))
        {
            throw new InvalidOperationException(
                "Only pending or failed outbox messages can be processed.");
        }

        Status = MessageStatus.Processing;
        AttemptCount++;
        Error = null;
        NextAttemptAt = null;
        ProcessedAt = null;
        UpdatedAt = processingAtUtc;
    }

    public void MarkSent(DateTime sentAtUtc)
    {
        if (Status != MessageStatus.Processing)
        {
            throw new InvalidOperationException(
                "Only processing outbox messages can be marked sent.");
        }

        Status = MessageStatus.Sent;
        Error = null;
        NextAttemptAt = null;
        ProcessedAt = sentAtUtc;
        UpdatedAt = sentAtUtc;
    }

    public void MarkFailed(
        string error,
        DateTime failedAtUtc,
        DateTime? nextAttemptAtUtc)
    {
        if (Status != MessageStatus.Processing)
        {
            throw new InvalidOperationException(
                "Only processing outbox messages can be marked failed.");
        }

        if (string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException("Outbox failure error is required.");
        }

        Status = MessageStatus.Failed;
        Error = error.Trim();
        NextAttemptAt = nextAttemptAtUtc;
        ProcessedAt = failedAtUtc;
        UpdatedAt = failedAtUtc;
    }

    public void Cancel(DateTime cancelledAtUtc, string? reason)
    {
        if (Status is MessageStatus.Sent or MessageStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Sent or cancelled outbox messages cannot be cancelled.");
        }

        Status = MessageStatus.Cancelled;
        Error = NormalizeOptional(reason);
        NextAttemptAt = null;
        ProcessedAt = cancelledAtUtc;
        UpdatedAt = cancelledAtUtc;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
