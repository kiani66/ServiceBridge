using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeOrderCreationRepository : IOrderCreationRepository
{
    public bool UserExists { get; set; } = true;
    public OrderServiceDefinition? ServiceDefinition { get; set; }
    public HashSet<string> ExistingOrderNumbers { get; } = new();
    public Order? AddedOrder { get; private set; }

    public Task<bool> UserExistsAsync(Guid userId)
    {
        return Task.FromResult(UserExists);
    }

    public Task<OrderServiceDefinition?> GetActiveServiceDefinitionAsync(
        Guid serviceId,
        Guid? planId)
    {
        if (ServiceDefinition != null && planId.HasValue)
        {
            ServiceDefinition.PlanId = planId;
        }

        return Task.FromResult(ServiceDefinition);
    }

    public Task<bool> OrderNumberExistsAsync(string orderNumber)
    {
        return Task.FromResult(ExistingOrderNumbers.Contains(orderNumber));
    }

    public Task AddOrderAsync(Order order)
    {
        AddedOrder = order;
        return Task.CompletedTask;
    }
}
