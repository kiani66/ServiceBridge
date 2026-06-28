using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IAgentOrderCommandRepository
{
    Task<Order?> GetAssignedOrderForUpdateAsync(Guid agentId, Guid orderId);
    Task<bool> SaveChangesAsync();
}
