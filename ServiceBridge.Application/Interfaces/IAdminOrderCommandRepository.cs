using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminOrderCommandRepository
{
    Task<Order?> GetOrderForAssignmentAsync(Guid orderId);
    Task<bool> ActiveAgentExistsAsync(Guid agentId);
    Task<bool> SaveChangesAsync();
}
