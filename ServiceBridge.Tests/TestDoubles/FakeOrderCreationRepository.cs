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
    public Guid? RequestedServiceVariantId { get; private set; }
    public Guid? RequestedServiceId { get; private set; }

    public Task<bool> UserExistsAsync(Guid userId)
    {
        return Task.FromResult(UserExists);
    }

    public Task<OrderServiceDefinition?> GetServiceVariantOrderDefinitionAsync(
        Guid serviceVariantId,
        Guid? serviceId)
    {
        RequestedServiceVariantId = serviceVariantId;
        RequestedServiceId = serviceId;

        if (ServiceDefinition == null)
        {
            return Task.FromResult<OrderServiceDefinition?>(null);
        }

        if (ServiceDefinition.ServiceVariantId != serviceVariantId)
        {
            return Task.FromResult<OrderServiceDefinition?>(null);
        }

        if (serviceId.HasValue && ServiceDefinition.ServiceId != serviceId.Value)
        {
            return Task.FromResult<OrderServiceDefinition?>(null);
        }

        return Task.FromResult<OrderServiceDefinition?>(ServiceDefinition);
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
