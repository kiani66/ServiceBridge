using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IFulfillmentExecutionRepository
{
    Task<bool> ExistsForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<FulfillmentExecution?> GetForMutationAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        FulfillmentExecution execution,
        CancellationToken cancellationToken = default);
}
