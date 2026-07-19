using ServiceBridge.Application.Fulfillment;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.Exceptions;
using ServiceBridge.Domain.ValueObjects;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public sealed class FulfillmentCoordinator : IFulfillmentCoordinator
{
    private const string ManualAgentFailureCode = "manual_agent_failed";
    private readonly IFulfillmentExecutionRepository _repository;

    public FulfillmentCoordinator(IFulfillmentExecutionRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<FulfillmentInitializationOutcome>>
        EnsureForPaidOrderAsync(
            Order order,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        var eligibility = GetManualEligibility(order);
        if (eligibility != ManualEligibility.Eligible)
        {
            return InitializationResult(eligibility);
        }

        if (await _repository.ExistsForOrderAsync(order.Id, cancellationToken))
        {
            return ServiceResult<FulfillmentInitializationOutcome>.Ok(
                FulfillmentInitializationOutcome.AlreadyExists);
        }

        if (order.Status != OrderStatus.Paid)
        {
            return InitializationConflict();
        }

        await _repository.AddAsync(
            FulfillmentExecution.Create(Guid.NewGuid(), order.Id),
            cancellationToken);

        return ServiceResult<FulfillmentInitializationOutcome>.Ok(
            FulfillmentInitializationOutcome.Created);
    }

    public async Task<ServiceResult> EnsureForManualAssignmentAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        var eligibility = GetManualEligibility(order);
        if (eligibility != ManualEligibility.Eligible)
        {
            return CompatibilityResult(eligibility);
        }

        if (order.Status != OrderStatus.Paid)
        {
            return Conflict();
        }

        if (!await _repository.ExistsForOrderAsync(order.Id, cancellationToken))
        {
            await _repository.AddAsync(
                FulfillmentExecution.Create(Guid.NewGuid(), order.Id),
                cancellationToken);
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> StartManualAttemptAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        var eligibility = GetManualEligibility(order);
        if (eligibility != ManualEligibility.Eligible)
        {
            return CompatibilityResult(eligibility);
        }

        if (order.Status != OrderStatus.Processing)
        {
            return Conflict();
        }

        var execution = await _repository.GetForMutationAsync(
            order.Id,
            cancellationToken);
        if (execution is null)
        {
            execution = FulfillmentExecution.Create(Guid.NewGuid(), order.Id);
            await _repository.AddAsync(execution, cancellationToken);
        }

        try
        {
            execution.StartAttempt(
                Guid.NewGuid(),
                FulfillmentMode.ManualAgent,
                DateTime.UtcNow);
            return ServiceResult.Ok();
        }
        catch (InvalidOperationException)
        {
            return Conflict();
        }
    }

    public Task<ServiceResult> CompleteManualAttemptAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        return FinishManualAttemptAsync(
            order,
            fail: false,
            cancellationToken);
    }

    public Task<ServiceResult> FailManualAttemptAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        return FinishManualAttemptAsync(
            order,
            fail: true,
            cancellationToken);
    }

    private async Task<ServiceResult> FinishManualAttemptAsync(
        Order order,
        bool fail,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);

        var eligibility = GetManualEligibility(order);
        if (eligibility != ManualEligibility.Eligible)
        {
            return CompatibilityResult(eligibility);
        }

        var expectedStatus = fail ? OrderStatus.Failed : OrderStatus.Completed;
        if (order.Status != expectedStatus)
        {
            return Conflict();
        }

        var execution = await _repository.GetForMutationAsync(
            order.Id,
            cancellationToken);
        if (execution is null)
        {
            return order.FulfillmentPolicyId is null
                ? ServiceResult.Ok()
                : Missing();
        }

        var activeAttempt = execution.Attempts.SingleOrDefault(attempt =>
            attempt.Status == FulfillmentAttemptStatus.Processing &&
            attempt.FulfillmentMode == FulfillmentMode.ManualAgent);
        if (activeAttempt is null)
        {
            return Conflict();
        }

        try
        {
            if (fail)
            {
                execution.FailAttempt(
                    activeAttempt.Id,
                    FulfillmentFailureDisposition.Terminal,
                    FulfillmentFailureCode.Create(ManualAgentFailureCode),
                    failureMessage: null,
                    DateTime.UtcNow);
            }
            else
            {
                execution.CompleteAttempt(activeAttempt.Id, DateTime.UtcNow);
            }

            return ServiceResult.Ok();
        }
        catch (InvalidOperationException)
        {
            return Conflict();
        }
    }

    private static ManualEligibility GetManualEligibility(Order order)
    {
        if (order.FulfillmentPolicyId is null)
        {
            return order.ServiceFulfillmentModeSnapshot == FulfillmentMode.ManualAgent
                ? ManualEligibility.Eligible
                : ManualEligibility.NotApplicable;
        }

        if (order.PolicyFulfillmentModeSnapshot is null ||
            order.PolicyFulfillmentModeSnapshot !=
            order.ServiceFulfillmentModeSnapshot)
        {
            return ManualEligibility.LegacyModeMismatch;
        }

        return order.PolicyFulfillmentModeSnapshot == FulfillmentMode.ManualAgent
            ? ManualEligibility.Eligible
            : ManualEligibility.NotApplicable;
    }

    private static ServiceResult<FulfillmentInitializationOutcome>
        InitializationResult(ManualEligibility eligibility)
    {
        var outcome = eligibility == ManualEligibility.LegacyModeMismatch
            ? FulfillmentInitializationOutcome.LegacyModeMismatch
            : FulfillmentInitializationOutcome.NotApplicable;

        return ServiceResult<FulfillmentInitializationOutcome>.Ok(outcome);
    }

    private static ServiceResult CompatibilityResult(ManualEligibility eligibility)
    {
        return eligibility == ManualEligibility.LegacyModeMismatch
            ? ServiceResult.Ok(AppMessages.FulfillmentModeMigrationMismatch)
            : ServiceResult.Ok();
    }

    private static ServiceResult<FulfillmentInitializationOutcome>
        InitializationConflict()
    {
        return ServiceResult<FulfillmentInitializationOutcome>.Fail(
            AppMessages.FulfillmentExecutionConflict,
            AppConstants.ErrorCodes.FulfillmentExecutionConflict,
            409);
    }

    private static ServiceResult Missing()
    {
        return ServiceResult.Fail(
            AppMessages.FulfillmentExecutionMissing,
            AppConstants.ErrorCodes.FulfillmentExecutionMissing,
            409);
    }

    private static ServiceResult Conflict()
    {
        return ServiceResult.Fail(
            AppMessages.FulfillmentExecutionConflict,
            AppConstants.ErrorCodes.FulfillmentExecutionConflict,
            409);
    }

    private enum ManualEligibility
    {
        Eligible,
        NotApplicable,
        LegacyModeMismatch
    }
}
