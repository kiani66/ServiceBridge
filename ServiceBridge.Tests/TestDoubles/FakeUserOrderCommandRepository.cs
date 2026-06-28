using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeUserOrderCommandRepository : IUserOrderCommandRepository
{
    public Order? Order { get; set; }
    public int SaveCount { get; private set; }
    public bool SaveSucceeds { get; set; } = true;

    public Task<Order?> GetOwnedOrderForUpdateAsync(Guid userId, Guid orderId)
    {
        var ownedOrder = Order?.UserId == userId && Order.Id == orderId
            ? Order
            : null;

        return Task.FromResult(ownedOrder);
    }

    public Task<bool> SaveChangesAsync()
    {
        SaveCount++;
        return Task.FromResult(SaveSucceeds);
    }
}
