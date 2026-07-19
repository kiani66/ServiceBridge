using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.Exceptions;
using ServiceBridge.Domain.ValueObjects;

namespace ServiceBridge.Domain.Entities;

public sealed class FulfillmentExecution
{
    private readonly List<FulfillmentAttempt> _attempts = new();

    private FulfillmentExecution()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public FulfillmentExecutionStatus Status { get; private set; }
    public int LastAttemptNumber { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public IReadOnlyCollection<FulfillmentAttempt> Attempts => _attempts.AsReadOnly();
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public Order Order { get; private set; } = null!;

    public static FulfillmentExecution Create(Guid executionId, Guid orderId)
    {
        if (executionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Fulfillment execution ID is required.",
                nameof(executionId));
        }

        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("Order ID is required.", nameof(orderId));
        }

        return new FulfillmentExecution
        {
            Id = executionId,
            OrderId = orderId,
            Status = FulfillmentExecutionStatus.Pending
        };
    }

    public FulfillmentAttempt StartAttempt(
        Guid attemptId,
        FulfillmentMode fulfillmentMode,
        DateTime startedAtUtc)
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException(
                "Fulfillment attempt ID is required.",
                nameof(attemptId));
        }

        EnsureDefined(fulfillmentMode, nameof(fulfillmentMode));
        EnsureTimestamp(startedAtUtc, nameof(startedAtUtc));

        if (Status != FulfillmentExecutionStatus.Pending)
        {
            throw new InvalidFulfillmentExecutionTransitionException(
                Status,
                nameof(StartAttempt));
        }

        if (_attempts.Any(attempt =>
                attempt.Status == FulfillmentAttemptStatus.Processing))
        {
            throw new InvalidOperationException(
                "A fulfillment attempt is already active.");
        }

        if (_attempts.Any(attempt => attempt.Id == attemptId))
        {
            throw new InvalidOperationException(
                "Fulfillment attempt ID has already been used by this execution.");
        }

        var previousCompletion = _attempts
            .OrderByDescending(attempt => attempt.AttemptNumber)
            .Select(attempt => attempt.CompletedAtUtc)
            .FirstOrDefault();
        if (previousCompletion.HasValue && startedAtUtc < previousCompletion.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startedAtUtc),
                "Attempt start timestamp cannot precede the previous attempt completion.");
        }

        var attemptNumber = checked(LastAttemptNumber + 1);
        var attempt = FulfillmentAttempt.Start(
            attemptId,
            Id,
            attemptNumber,
            fulfillmentMode,
            startedAtUtc);

        _attempts.Add(attempt);
        LastAttemptNumber = attemptNumber;
        StartedAtUtc ??= startedAtUtc;
        Status = FulfillmentExecutionStatus.Processing;

        return attempt;
    }

    public void CompleteAttempt(Guid attemptId, DateTime completedAtUtc)
    {
        var attempt = GetOwnedAttempt(attemptId);
        EnsureAttemptIsActive(attempt);

        attempt.MarkSucceeded(completedAtUtc);
        Status = FulfillmentExecutionStatus.Completed;
        CompletedAtUtc = completedAtUtc;
    }

    public void FailAttempt(
        Guid attemptId,
        FulfillmentFailureDisposition disposition,
        FulfillmentFailureCode failureCode,
        string? failureMessage,
        DateTime completedAtUtc)
    {
        EnsureDefined(disposition, nameof(disposition));
        ArgumentNullException.ThrowIfNull(failureCode);

        var attempt = GetOwnedAttempt(attemptId);
        EnsureAttemptIsActive(attempt);

        attempt.MarkFailed(
            disposition,
            failureCode,
            failureMessage,
            completedAtUtc);

        if (disposition == FulfillmentFailureDisposition.Retryable)
        {
            Status = FulfillmentExecutionStatus.Pending;
            return;
        }

        Status = FulfillmentExecutionStatus.Failed;
        CompletedAtUtc = completedAtUtc;
    }

    private FulfillmentAttempt GetOwnedAttempt(Guid attemptId)
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException(
                "Fulfillment attempt ID is required.",
                nameof(attemptId));
        }

        return _attempts.SingleOrDefault(attempt => attempt.Id == attemptId)
            ?? throw new InvalidOperationException(
                "Fulfillment attempt does not belong to this execution.");
    }

    private void EnsureAttemptIsActive(FulfillmentAttempt attempt)
    {
        if (attempt.Status != FulfillmentAttemptStatus.Processing)
        {
            throw new InvalidOperationException(
                "A terminal fulfillment attempt cannot be changed.");
        }

        if (Status != FulfillmentExecutionStatus.Processing)
        {
            throw new InvalidFulfillmentExecutionTransitionException(
                Status,
                "finish an attempt");
        }
    }

    private static void EnsureTimestamp(DateTime value, string parameterName)
    {
        if (value == default)
        {
            throw new ArgumentException("Timestamp is required.", parameterName);
        }
    }

    private static void EnsureDefined<TEnum>(TEnum value, string parameterName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Unsupported {typeof(TEnum).Name} value.");
        }
    }
}
