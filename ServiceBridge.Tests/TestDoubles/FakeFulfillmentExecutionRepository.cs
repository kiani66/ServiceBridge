using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakeFulfillmentExecutionRepository :
    IFulfillmentExecutionRepository
{
    public List<FulfillmentExecution> Executions { get; } = new();

    public Task<bool> ExistsForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Executions.Any(x => x.OrderId == orderId));
    }

    public Task<FulfillmentExecution?> GetForMutationAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            Executions.SingleOrDefault(x => x.OrderId == orderId));
    }

    public Task AddAsync(
        FulfillmentExecution execution,
        CancellationToken cancellationToken = default)
    {
        Executions.Add(execution);
        return Task.CompletedTask;
    }
}
