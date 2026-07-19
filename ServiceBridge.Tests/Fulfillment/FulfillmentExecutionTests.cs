using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.Exceptions;
using ServiceBridge.Domain.ValueObjects;
using Xunit;

namespace ServiceBridge.Tests.Fulfillment;

public sealed class FulfillmentExecutionTests
{
    private static readonly DateTime StartedAtUtc =
        new(2026, 7, 19, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ProducesPendingExecutionWithClientIdentifiers()
    {
        var executionId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var execution = FulfillmentExecution.Create(executionId, orderId);

        Assert.Equal(executionId, execution.Id);
        Assert.Equal(orderId, execution.OrderId);
        Assert.Equal(FulfillmentExecutionStatus.Pending, execution.Status);
        Assert.Equal(0, execution.LastAttemptNumber);
        Assert.Null(execution.StartedAtUtc);
        Assert.Null(execution.CompletedAtUtc);
        Assert.Empty(execution.Attempts);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_WithEmptyIdentifier_Throws(bool emptyExecutionId)
    {
        Assert.Throws<ArgumentException>(() => FulfillmentExecution.Create(
            emptyExecutionId ? Guid.Empty : Guid.NewGuid(),
            emptyExecutionId ? Guid.NewGuid() : Guid.Empty));
    }

    [Fact]
    public void StartAttempt_FirstAttemptStartsAtOne()
    {
        var execution = CreateExecution();
        var attemptId = Guid.NewGuid();

        var attempt = execution.StartAttempt(
            attemptId,
            FulfillmentMode.ManualAgent,
            StartedAtUtc);

        Assert.Equal(attemptId, attempt.Id);
        Assert.Equal(execution.Id, attempt.FulfillmentExecutionId);
        Assert.Equal(1, attempt.AttemptNumber);
        Assert.Equal(FulfillmentAttemptStatus.Processing, attempt.Status);
        Assert.Equal(FulfillmentExecutionStatus.Processing, execution.Status);
        Assert.Equal(StartedAtUtc, execution.StartedAtUtc);
    }

    [Fact]
    public void StartAttempt_AfterRetryableFailure_IncrementsMonotonically()
    {
        var execution = CreateExecution();
        var firstId = Guid.NewGuid();
        execution.StartAttempt(firstId, FulfillmentMode.ManualAgent, StartedAtUtc);
        execution.FailAttempt(
            firstId,
            FulfillmentFailureDisposition.Retryable,
            Code(),
            "try again",
            StartedAtUtc.AddMinutes(1));

        var second = execution.StartAttempt(
            Guid.NewGuid(),
            FulfillmentMode.InventoryCode,
            StartedAtUtc.AddMinutes(2));

        Assert.Equal(2, second.AttemptNumber);
        Assert.Equal(2, execution.LastAttemptNumber);
        Assert.Equal(2, execution.Attempts.Count);
    }

    [Fact]
    public void StartAttempt_WhileAnotherAttemptIsActive_Throws()
    {
        var execution = CreateExecution();
        execution.StartAttempt(Guid.NewGuid(), FulfillmentMode.ManualAgent, StartedAtUtc);

        Assert.Throws<InvalidFulfillmentExecutionTransitionException>(() =>
            execution.StartAttempt(
                Guid.NewGuid(),
                FulfillmentMode.ManualAgent,
                StartedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void StartAttempt_WithUndefinedMode_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateExecution().StartAttempt(
            Guid.NewGuid(),
            (FulfillmentMode)999,
            StartedAtUtc));
    }

    [Fact]
    public void CompleteAttempt_CompletesAttemptAndExecutionWithoutFailureMetadata()
    {
        var execution = CreateExecution();
        var attemptId = Guid.NewGuid();
        var attempt = execution.StartAttempt(
            attemptId,
            FulfillmentMode.ManualAgent,
            StartedAtUtc);
        var completedAtUtc = StartedAtUtc.AddMinutes(1);

        execution.CompleteAttempt(attemptId, completedAtUtc);

        Assert.Equal(FulfillmentExecutionStatus.Completed, execution.Status);
        Assert.Equal(completedAtUtc, execution.CompletedAtUtc);
        Assert.Equal(FulfillmentAttemptStatus.Succeeded, attempt.Status);
        Assert.Equal(completedAtUtc, attempt.CompletedAtUtc);
        Assert.Null(attempt.FailureDisposition);
        Assert.Null(attempt.FailureCode);
        Assert.Null(attempt.FailureMessage);
    }

    [Fact]
    public void FailAttempt_WithRetryableDisposition_ReturnsExecutionToPending()
    {
        var execution = CreateExecution();
        var attemptId = Guid.NewGuid();
        var attempt = execution.StartAttempt(
            attemptId,
            FulfillmentMode.ExternalAutomation,
            StartedAtUtc);

        execution.FailAttempt(
            attemptId,
            FulfillmentFailureDisposition.Retryable,
            Code(),
            "  temporary\r\n failure  ",
            StartedAtUtc.AddMinutes(1));

        Assert.Equal(FulfillmentExecutionStatus.Pending, execution.Status);
        Assert.Null(execution.CompletedAtUtc);
        Assert.Equal(FulfillmentAttemptStatus.Failed, attempt.Status);
        Assert.Equal(FulfillmentFailureDisposition.Retryable, attempt.FailureDisposition);
        Assert.Equal("temporary failure", attempt.FailureMessage);
    }

    [Fact]
    public void FailAttempt_WithTerminalDisposition_FailsExecution()
    {
        var execution = CreateExecution();
        var attemptId = Guid.NewGuid();
        var attempt = execution.StartAttempt(
            attemptId,
            FulfillmentMode.ManualAgent,
            StartedAtUtc);
        var completedAtUtc = StartedAtUtc.AddMinutes(1);

        execution.FailAttempt(
            attemptId,
            FulfillmentFailureDisposition.Terminal,
            Code(),
            null,
            completedAtUtc);

        Assert.Equal(FulfillmentExecutionStatus.Failed, execution.Status);
        Assert.Equal(completedAtUtc, execution.CompletedAtUtc);
        Assert.Equal(FulfillmentFailureDisposition.Terminal, attempt.FailureDisposition);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TerminalExecution_CannotStartAnotherAttempt(bool terminalFailure)
    {
        var execution = CreateExecution();
        var attemptId = Guid.NewGuid();
        execution.StartAttempt(attemptId, FulfillmentMode.ManualAgent, StartedAtUtc);
        if (terminalFailure)
        {
            execution.FailAttempt(
                attemptId,
                FulfillmentFailureDisposition.Terminal,
                Code(),
                null,
                StartedAtUtc.AddMinutes(1));
        }
        else
        {
            execution.CompleteAttempt(attemptId, StartedAtUtc.AddMinutes(1));
        }

        Assert.Throws<InvalidFulfillmentExecutionTransitionException>(() =>
            execution.StartAttempt(
                Guid.NewGuid(),
                FulfillmentMode.ManualAgent,
                StartedAtUtc.AddMinutes(2)));
    }

    [Fact]
    public void TerminalAttempt_CannotFinishTwice()
    {
        var execution = CreateExecution();
        var attemptId = Guid.NewGuid();
        execution.StartAttempt(attemptId, FulfillmentMode.ManualAgent, StartedAtUtc);
        execution.FailAttempt(
            attemptId,
            FulfillmentFailureDisposition.Retryable,
            Code(),
            null,
            StartedAtUtc.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => execution.CompleteAttempt(
            attemptId,
            StartedAtUtc.AddMinutes(2)));
    }

    [Fact]
    public void ForeignAttemptId_IsRejected()
    {
        var execution = CreateExecution();
        execution.StartAttempt(Guid.NewGuid(), FulfillmentMode.ManualAgent, StartedAtUtc);

        Assert.Throws<InvalidOperationException>(() => execution.CompleteAttempt(
            Guid.NewGuid(),
            StartedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void CompletionBeforeAttemptStart_IsRejected()
    {
        var execution = CreateExecution();
        var attemptId = Guid.NewGuid();
        execution.StartAttempt(attemptId, FulfillmentMode.ManualAgent, StartedAtUtc);

        Assert.Throws<ArgumentOutOfRangeException>(() => execution.CompleteAttempt(
            attemptId,
            StartedAtUtc.AddTicks(-1)));
    }

    [Fact]
    public void RetryStartBeforePreviousCompletion_IsRejected()
    {
        var execution = CreateExecution();
        var attemptId = Guid.NewGuid();
        execution.StartAttempt(attemptId, FulfillmentMode.ManualAgent, StartedAtUtc);
        execution.FailAttempt(
            attemptId,
            FulfillmentFailureDisposition.Retryable,
            Code(),
            null,
            StartedAtUtc.AddMinutes(2));

        Assert.Throws<ArgumentOutOfRangeException>(() => execution.StartAttempt(
            Guid.NewGuid(),
            FulfillmentMode.ManualAgent,
            StartedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void FailAttempt_RequiresFailureCode()
    {
        var execution = CreateExecution();
        var attemptId = Guid.NewGuid();
        execution.StartAttempt(attemptId, FulfillmentMode.ManualAgent, StartedAtUtc);

        Assert.Throws<ArgumentNullException>(() => execution.FailAttempt(
            attemptId,
            FulfillmentFailureDisposition.Retryable,
            null!,
            null,
            StartedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void FailAttempt_RejectsUndefinedDisposition()
    {
        var execution = CreateExecution();
        var attemptId = Guid.NewGuid();
        execution.StartAttempt(attemptId, FulfillmentMode.ManualAgent, StartedAtUtc);

        Assert.Throws<ArgumentOutOfRangeException>(() => execution.FailAttempt(
            attemptId,
            (FulfillmentFailureDisposition)999,
            Code(),
            null,
            StartedAtUtc.AddMinutes(1)));
    }

    [Fact]
    public void FailureMessageBeyondBound_IsRejectedWithoutChangingAttempt()
    {
        var execution = CreateExecution();
        var attemptId = Guid.NewGuid();
        var attempt = execution.StartAttempt(
            attemptId,
            FulfillmentMode.ManualAgent,
            StartedAtUtc);

        Assert.Throws<ArgumentOutOfRangeException>(() => execution.FailAttempt(
            attemptId,
            FulfillmentFailureDisposition.Retryable,
            Code(),
            new string('x', FulfillmentAttempt.FailureMessageMaxLength + 1),
            StartedAtUtc.AddMinutes(1)));
        Assert.Equal(FulfillmentAttemptStatus.Processing, attempt.Status);
        Assert.Equal(FulfillmentExecutionStatus.Processing, execution.Status);
    }

    [Fact]
    public void FailureCode_IsBoundedAndMachineReadable()
    {
        Assert.Equal("Inventory.Unavailable_1", Code().Value);
        Assert.Throws<ArgumentException>(() => FulfillmentFailureCode.Create(" "));
        Assert.Throws<ArgumentException>(() =>
            FulfillmentFailureCode.Create("unsafe code"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FulfillmentFailureCode.Create(
                new string('A', FulfillmentFailureCode.MaxLength + 1)));
    }

    [Fact]
    public void StatePropertiesHaveNoPublicSettersOrConstructors()
    {
        Assert.Empty(typeof(FulfillmentExecution).GetConstructors());
        Assert.Empty(typeof(FulfillmentAttempt).GetConstructors());

        Assert.All(
            typeof(FulfillmentExecution).GetProperties(),
            property => Assert.True(property.SetMethod is null || !property.SetMethod.IsPublic));
        Assert.All(
            typeof(FulfillmentAttempt).GetProperties(),
            property => Assert.True(property.SetMethod is null || !property.SetMethod.IsPublic));
    }

    [Fact]
    public void ExecutionEnumValues_AreStable()
    {
        Assert.Equal(1, (int)FulfillmentExecutionStatus.Pending);
        Assert.Equal(2, (int)FulfillmentExecutionStatus.Processing);
        Assert.Equal(3, (int)FulfillmentExecutionStatus.Completed);
        Assert.Equal(4, (int)FulfillmentExecutionStatus.Failed);
        Assert.Equal(1, (int)FulfillmentAttemptStatus.Processing);
        Assert.Equal(2, (int)FulfillmentAttemptStatus.Succeeded);
        Assert.Equal(3, (int)FulfillmentAttemptStatus.Failed);
        Assert.Equal(1, (int)FulfillmentFailureDisposition.Retryable);
        Assert.Equal(2, (int)FulfillmentFailureDisposition.Terminal);
    }

    private static FulfillmentExecution CreateExecution()
    {
        return FulfillmentExecution.Create(Guid.NewGuid(), Guid.NewGuid());
    }

    private static FulfillmentFailureCode Code()
    {
        return FulfillmentFailureCode.Create("Inventory.Unavailable_1");
    }
}
