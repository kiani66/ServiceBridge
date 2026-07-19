using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.ValueObjects;

namespace ServiceBridge.Domain.Entities;

public sealed class FulfillmentAttempt
{
    public const int FailureMessageMaxLength = 500;

    private FulfillmentAttempt()
    {
    }

    public Guid Id { get; private set; }
    public Guid FulfillmentExecutionId { get; private set; }
    public int AttemptNumber { get; private set; }
    public FulfillmentMode FulfillmentMode { get; private set; }
    public FulfillmentAttemptStatus Status { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public FulfillmentFailureDisposition? FailureDisposition { get; private set; }
    public FulfillmentFailureCode? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }

    internal static FulfillmentAttempt Start(
        Guid id,
        Guid executionId,
        int attemptNumber,
        FulfillmentMode fulfillmentMode,
        DateTime startedAtUtc)
    {
        return new FulfillmentAttempt
        {
            Id = id,
            FulfillmentExecutionId = executionId,
            AttemptNumber = attemptNumber,
            FulfillmentMode = fulfillmentMode,
            Status = FulfillmentAttemptStatus.Processing,
            StartedAtUtc = startedAtUtc
        };
    }

    internal void MarkSucceeded(DateTime completedAtUtc)
    {
        EnsureProcessing();
        EnsureCompletionTimestamp(completedAtUtc);

        Status = FulfillmentAttemptStatus.Succeeded;
        CompletedAtUtc = completedAtUtc;
    }

    internal void MarkFailed(
        FulfillmentFailureDisposition disposition,
        FulfillmentFailureCode failureCode,
        string? failureMessage,
        DateTime completedAtUtc)
    {
        EnsureProcessing();
        EnsureCompletionTimestamp(completedAtUtc);
        var sanitizedFailureMessage = SanitizeFailureMessage(failureMessage);

        Status = FulfillmentAttemptStatus.Failed;
        CompletedAtUtc = completedAtUtc;
        FailureDisposition = disposition;
        FailureCode = failureCode;
        FailureMessage = sanitizedFailureMessage;
    }

    private void EnsureProcessing()
    {
        if (Status != FulfillmentAttemptStatus.Processing)
        {
            throw new InvalidOperationException(
                "A terminal fulfillment attempt cannot be changed.");
        }
    }

    private void EnsureCompletionTimestamp(DateTime completedAtUtc)
    {
        if (completedAtUtc == default)
        {
            throw new ArgumentException(
                "Completion timestamp is required.",
                nameof(completedAtUtc));
        }

        if (completedAtUtc < StartedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(completedAtUtc),
                "Completion timestamp cannot precede the attempt start timestamp.");
        }
    }

    private static string? SanitizeFailureMessage(string? failureMessage)
    {
        if (string.IsNullOrWhiteSpace(failureMessage))
        {
            return null;
        }

        var sanitized = string.Join(
            ' ',
            failureMessage.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

        if (sanitized.Length > FailureMessageMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failureMessage),
                $"Failure message cannot exceed {FailureMessageMaxLength} characters.");
        }

        return sanitized;
    }
}
