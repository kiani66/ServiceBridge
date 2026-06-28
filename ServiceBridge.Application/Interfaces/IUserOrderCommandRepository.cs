using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IUserOrderCommandRepository
{
    Task<Order?> GetOwnedOrderForUpdateAsync(Guid userId, Guid orderId);
    Task<bool> SaveChangesAsync();
}
