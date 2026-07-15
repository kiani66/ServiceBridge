using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminOrderCommandRepository
{
    Task<Order?> GetOrderForAssignmentAsync(Guid orderId);
    Task<Order?> LockOrderForInventoryFulfillmentAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
    Task<bool> ActiveAgentExistsAsync(Guid agentId);
    Task<bool> SaveChangesAsync();
}
