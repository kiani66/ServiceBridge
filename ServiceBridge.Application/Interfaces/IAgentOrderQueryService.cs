using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface IAgentOrderQueryService
{
    const int DefaultPageSize = 20;
    const int MaximumPageSize = 100;

    Task<ServiceResult<AgentOrderDashboardSummaryDto>>
        GetDashboardSummaryAsync(Guid agentId);

    Task<ServiceResult<PagedResultDto<AgentOrderListItemDto>>>
        GetAssignedOrdersAsync(
            Guid agentId,
            int page,
            int pageSize,
            OrderStatus? status,
            string? search);

    Task<ServiceResult<AgentOrderDetailDto>> GetAssignedOrderDetailAsync(
        Guid agentId,
        Guid orderId);
}
