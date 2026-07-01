using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Infrastructure.Services;

public class MessageOutboxService : IMessageOutboxService
{
    private const int MaximumRecipientLength = 320;
    private const int MaximumSubjectLength = 200;
    private const int MaximumRelatedEntityTypeLength = 80;
    private const int MaximumErrorLength = 1000;

    private readonly IMessageOutboxRepository _repository;

    public MessageOutboxService(IMessageOutboxRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<MessageOutboxDto>> EnqueueAsync(
        EnqueueMessageRequest request)
    {
        var validationError = ValidateEnqueueRequest(request);
        if (validationError != null)
        {
            return ValidationFailure(validationError);
        }

        var message = MessageOutbox.Create(
            request.Channel,
            request.Recipient,
            request.Subject,
            request.Body,
            request.UserId,
            request.RelatedEntityType,
            request.RelatedEntityId,
            DateTime.UtcNow);

        await _repository.AddAsync(message);

        if (!await _repository.SaveChangesAsync())
        {
            return Conflict("Message outbox entry changed while being created.");
        }

        return ServiceResult<MessageOutboxDto>.Created(
            Map(message),
            "Message queued in outbox.");
    }

    public Task<ServiceResult<MessageOutboxDto>> MarkProcessingAsync(Guid id)
    {
        return UpdateStatusAsync(
            id,
            message => message.MarkProcessing(DateTime.UtcNow),
            "Message outbox entry marked processing.");
    }

    public Task<ServiceResult<MessageOutboxDto>> MarkSentAsync(Guid id)
    {
        return UpdateStatusAsync(
            id,
            message => message.MarkSent(DateTime.UtcNow),
            "Message outbox entry marked sent.");
    }

    public Task<ServiceResult<MessageOutboxDto>> MarkFailedAsync(
        Guid id,
        string error,
        DateTime? nextAttemptAtUtc)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return Task.FromResult(ValidationFailure("Error is required."));
        }

        if (error.Trim().Length > MaximumErrorLength)
        {
            return Task.FromResult(ValidationFailure(
                $"Error cannot exceed {MaximumErrorLength} characters."));
        }

        return UpdateStatusAsync(
            id,
            message => message.MarkFailed(error, DateTime.UtcNow, nextAttemptAtUtc),
            "Message outbox entry marked failed.");
    }

    public Task<ServiceResult<MessageOutboxDto>> CancelAsync(Guid id, string? reason)
    {
        if (!string.IsNullOrWhiteSpace(reason) &&
            reason.Trim().Length > MaximumErrorLength)
        {
            return Task.FromResult(ValidationFailure(
                $"Reason cannot exceed {MaximumErrorLength} characters."));
        }

        return UpdateStatusAsync(
            id,
            message => message.Cancel(DateTime.UtcNow, reason),
            "Message outbox entry cancelled.");
    }

    private async Task<ServiceResult<MessageOutboxDto>> UpdateStatusAsync(
        Guid id,
        Action<MessageOutbox> update,
        string successMessage)
    {
        var message = await _repository.GetForUpdateAsync(id);
        if (message == null)
        {
            return ServiceResult<MessageOutboxDto>.Fail(
                "Message outbox entry not found.",
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        try
        {
            update(message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }

        if (!await _repository.SaveChangesAsync())
        {
            return Conflict("Message outbox entry changed while being updated.");
        }

        return ServiceResult<MessageOutboxDto>.Ok(Map(message), successMessage);
    }

    private static string? ValidateEnqueueRequest(EnqueueMessageRequest request)
    {
        if (!Enum.IsDefined(request.Channel))
        {
            return "Message channel is invalid.";
        }

        if (string.IsNullOrWhiteSpace(request.Recipient))
        {
            return "Recipient is required.";
        }

        if (request.Recipient.Trim().Length > MaximumRecipientLength)
        {
            return $"Recipient cannot exceed {MaximumRecipientLength} characters.";
        }

        if (!string.IsNullOrWhiteSpace(request.Subject) &&
            request.Subject.Trim().Length > MaximumSubjectLength)
        {
            return $"Subject cannot exceed {MaximumSubjectLength} characters.";
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return "Body is required.";
        }

        if (!string.IsNullOrWhiteSpace(request.RelatedEntityType) &&
            request.RelatedEntityType.Trim().Length > MaximumRelatedEntityTypeLength)
        {
            return $"Related entity type cannot exceed {MaximumRelatedEntityTypeLength} characters.";
        }

        var hasRelatedEntityType = !string.IsNullOrWhiteSpace(request.RelatedEntityType);
        var hasRelatedEntityId = request.RelatedEntityId.HasValue;
        if (hasRelatedEntityType != hasRelatedEntityId)
        {
            return "Related entity type and ID must be supplied together.";
        }

        return null;
    }

    private static ServiceResult<MessageOutboxDto> ValidationFailure(string message)
    {
        return ServiceResult<MessageOutboxDto>.Fail(
            message,
            AppConstants.ErrorCodes.ValidationFailed,
            400);
    }

    private static ServiceResult<MessageOutboxDto> Conflict(string message)
    {
        return ServiceResult<MessageOutboxDto>.Fail(
            message,
            AppConstants.ErrorCodes.Conflict,
            409);
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
