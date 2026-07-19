using ServiceBridge.Application.Fulfillment;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IFulfillmentCoordinator
{
    Task<ServiceResult<FulfillmentInitializationOutcome>> EnsureForPaidOrderAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> EnsureForManualAssignmentAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> StartManualAttemptAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> CompleteManualAttemptAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> FailManualAttemptAsync(
        Order order,
        CancellationToken cancellationToken = default);
}
