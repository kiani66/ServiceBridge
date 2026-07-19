using ServiceBridge.Application.Fulfillment;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakeFulfillmentCoordinator : IFulfillmentCoordinator
{
    public ServiceResult<FulfillmentInitializationOutcome> InitializationResult { get; set; } =
        ServiceResult<FulfillmentInitializationOutcome>.Ok(
            FulfillmentInitializationOutcome.NotApplicable);

    public ServiceResult AssignmentResult { get; set; } = ServiceResult.Ok();
    public ServiceResult StartResult { get; set; } = ServiceResult.Ok();
    public ServiceResult CompleteResult { get; set; } = ServiceResult.Ok();
    public ServiceResult FailResult { get; set; } = ServiceResult.Ok();

    public int EnsurePaidCount { get; private set; }
    public int EnsureAssignmentCount { get; private set; }
    public int StartCount { get; private set; }
    public int CompleteCount { get; private set; }
    public int FailCount { get; private set; }

    public Task<ServiceResult<FulfillmentInitializationOutcome>>
        EnsureForPaidOrderAsync(
            Order order,
            CancellationToken cancellationToken = default)
    {
        EnsurePaidCount++;
        return Task.FromResult(InitializationResult);
    }

    public Task<ServiceResult> EnsureForManualAssignmentAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        EnsureAssignmentCount++;
        return Task.FromResult(AssignmentResult);
    }

    public Task<ServiceResult> StartManualAttemptAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        StartCount++;
        return Task.FromResult(StartResult);
    }

    public Task<ServiceResult> CompleteManualAttemptAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        CompleteCount++;
        return Task.FromResult(CompleteResult);
    }

    public Task<ServiceResult> FailManualAttemptAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        FailCount++;
        return Task.FromResult(FailResult);
    }
}
