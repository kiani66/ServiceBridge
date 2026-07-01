using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Application.Settings;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Infrastructure.Services;

public class MessageOutboxProcessor : IMessageOutboxProcessor
{
    private const int MaximumErrorLength = 1000;

    private readonly IMessageOutboxRepository _repository;
    private readonly IMessageDeliveryProvider _provider;
    private readonly MessageOutboxSettings _settings;
    private readonly ILogger<MessageOutboxProcessor> _logger;

    public MessageOutboxProcessor(
        IMessageOutboxRepository repository,
        IMessageDeliveryProvider provider,
        IOptions<MessageOutboxSettings> settings,
        ILogger<MessageOutboxProcessor> logger)
    {
        _repository = repository;
        _provider = provider;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<MessageOutboxProcessResult>> ProcessPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new MessageOutboxProcessResult();
        var batchSize = Math.Max(1, _settings.BatchSize);
        var maxAttempts = Math.Max(1, _settings.MaxAttempts);
        var now = DateTime.UtcNow;
        var messages = await _repository.GetDuePendingAsync(
            now,
            batchSize,
            cancellationToken);
        var seenMessageIds = new HashSet<Guid>();

        result.Picked = messages.Count;

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!seenMessageIds.Add(message.Id))
            {
                result.Skipped++;
                continue;
            }

            if (!CanProcess(message, now))
            {
                result.Skipped++;
                continue;
            }

            try
            {
                message.MarkProcessing(DateTime.UtcNow);
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Message outbox entry {MessageId} could not be marked processing.",
                    message.Id);
                result.Skipped++;
                continue;
            }

            if (!await _repository.SaveChangesAsync(cancellationToken))
            {
                result.Skipped++;
                continue;
            }

            var deliveryResult = await SendAsync(message, cancellationToken);
            ApplyDeliveryResult(message, deliveryResult, maxAttempts, result);

            if (!await _repository.SaveChangesAsync(cancellationToken))
            {
                return ServiceResult<MessageOutboxProcessResult>.Fail(
                    "Message outbox entry changed while being processed.",
                    AppConstants.ErrorCodes.Conflict,
                    409);
            }

            result.Processed++;
        }

        return ServiceResult<MessageOutboxProcessResult>.Ok(
            result,
            "Message outbox batch processed.");
    }

    private async Task<ServiceResult> SendAsync(
        MessageOutbox message,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _provider.SendAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Message delivery provider failed for outbox entry {MessageId}.",
                message.Id);

            return ServiceResult.Fail(
                exception.Message,
                AppConstants.ErrorCodes.Conflict,
                409);
        }
    }

    private static bool CanProcess(MessageOutbox message, DateTime now)
    {
        return message.Status == MessageStatus.Pending &&
            (message.NextAttemptAt == null || message.NextAttemptAt <= now);
    }

    private static void ApplyDeliveryResult(
        MessageOutbox message,
        ServiceResult deliveryResult,
        int maxAttempts,
        MessageOutboxProcessResult result)
    {
        var completedAt = DateTime.UtcNow;

        if (deliveryResult.Success)
        {
            message.MarkSent(completedAt);
            result.Sent++;
            return;
        }

        var error = NormalizeError(deliveryResult.Message);
        if (message.AttemptCount >= maxAttempts)
        {
            message.MarkFailed(error, completedAt, null);
            result.Failed++;
            return;
        }

        message.MarkPendingRetry(error, completedAt, CalculateNextAttemptAt(
            completedAt,
            message.AttemptCount));
        result.Retried++;
    }

    private static DateTime CalculateNextAttemptAt(
        DateTime failedAtUtc,
        int attemptCount)
    {
        var delayMinutes = Math.Min(60, Math.Pow(2, Math.Max(0, attemptCount - 1)));
        return failedAtUtc.AddMinutes(delayMinutes);
    }

    private static string NormalizeError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Message delivery failed.";
        }

        var trimmed = message.Trim();
        return trimmed.Length <= MaximumErrorLength
            ? trimmed
            : trimmed[..MaximumErrorLength];
    }
}
