using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface IAgentOrderQueryRepository
{
    Task<PagedResultDto<AgentOrderListItemDto>> GetAssignedOrdersAsync(
        Guid agentId,
        int page,
        int pageSize,
        OrderStatus? status);

    Task<AgentOrderDetailDto?> GetAssignedOrderDetailAsync(
        Guid agentId,
        Guid orderId);
}
