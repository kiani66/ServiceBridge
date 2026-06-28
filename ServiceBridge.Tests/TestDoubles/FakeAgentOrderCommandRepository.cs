using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeAgentOrderCommandRepository : IAgentOrderCommandRepository
{
    public Order? Order { get; set; }
    public bool SaveSucceeds { get; set; } = true;
    public int SaveCount { get; private set; }

    public Task<Order?> GetAssignedOrderForUpdateAsync(
        Guid agentId,
        Guid orderId)
    {
        var result = Order?.Id == orderId && Order.AssignedAgentId == agentId
            ? Order
            : null;
        return Task.FromResult(result);
    }

    public Task<bool> SaveChangesAsync()
    {
        SaveCount++;
        return Task.FromResult(SaveSucceeds);
    }
}
