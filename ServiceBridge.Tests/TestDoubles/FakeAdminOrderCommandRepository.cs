using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeAdminOrderCommandRepository : IAdminOrderCommandRepository
{
    public Order? Order { get; set; }
    public bool AgentExists { get; set; } = true;
    public bool SaveSucceeds { get; set; } = true;
    public int SaveCount { get; private set; }
    public int LockCount { get; private set; }
    public IList<string>? Operations { get; set; }

    public Task<Order?> GetOrderForAssignmentAsync(Guid orderId)
    {
        return Task.FromResult(Order?.Id == orderId ? Order : null);
    }

    public Task<Order?> LockOrderForInventoryFulfillmentAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        LockCount++;
        Operations?.Add("order.lock");
        return Task.FromResult(Order?.Id == orderId ? Order : null);
    }

    public Task<bool> ActiveAgentExistsAsync(Guid agentId)
    {
        return Task.FromResult(AgentExists);
    }

    public Task<bool> SaveChangesAsync()
    {
        SaveCount++;
        Operations?.Add("order.save");
        return Task.FromResult(SaveSucceeds);
    }
}
