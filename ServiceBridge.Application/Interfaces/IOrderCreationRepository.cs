using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface IOrderCreationRepository
{
    Task<bool> UserExistsAsync(Guid userId);
    Task<OrderServiceDefinition?> GetActiveServiceDefinitionAsync(
        Guid serviceId,
        Guid? planId);
    Task<bool> OrderNumberExistsAsync(string orderNumber);
    Task AddOrderAsync(Order order);
}
