using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface IAgentOrderQueryRepository
{
    Task<AgentOrderDashboardSummaryDto> GetDashboardSummaryAsync(
        Guid agentId,
        DateTime todayStartUtc,
        DateTime tomorrowStartUtc);

    Task<PagedResultDto<AgentOrderListItemDto>> GetAssignedOrdersAsync(
        Guid agentId,
        int page,
        int pageSize,
        OrderStatus? status,
        string? search);

    Task<AgentOrderDetailDto?> GetAssignedOrderDetailAsync(
        Guid agentId,
        Guid orderId);
}
