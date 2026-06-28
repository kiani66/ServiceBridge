using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IOrderCreationService
{
    Task<ServiceResult<OrderCreatedResponse>> CreateAsync(
        Guid userId,
        CreateOrderRequest request);
}
