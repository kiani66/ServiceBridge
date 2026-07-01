using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class AgentOrderQueryService : IAgentOrderQueryService
{
    private readonly IAgentOrderQueryRepository _repository;

    public AgentOrderQueryService(IAgentOrderQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<AgentOrderDashboardSummaryDto>>
        GetDashboardSummaryAsync(Guid agentId)
    {
        var todayStartUtc = DateTime.UtcNow.Date;
        var tomorrowStartUtc = todayStartUtc.AddDays(1);
        var summary = await _repository.GetDashboardSummaryAsync(
            agentId,
            todayStartUtc,
            tomorrowStartUtc);

        return ServiceResult<AgentOrderDashboardSummaryDto>.Ok(summary);
    }

    public async Task<ServiceResult<PagedResultDto<AgentOrderListItemDto>>>
        GetAssignedOrdersAsync(
            Guid agentId,
            int page,
            int pageSize,
            OrderStatus? status,
            string? search)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0
            ? IAgentOrderQueryService.DefaultPageSize
            : Math.Min(pageSize, IAgentOrderQueryService.MaximumPageSize);
        search = string.IsNullOrWhiteSpace(search)
            ? null
            : search.Trim();

        var orders = await _repository.GetAssignedOrdersAsync(
            agentId,
            page,
            pageSize,
            status,
            search);
        return ServiceResult<PagedResultDto<AgentOrderListItemDto>>.Ok(orders);
    }

    public async Task<ServiceResult<AgentOrderDetailDto>>
        GetAssignedOrderDetailAsync(Guid agentId, Guid orderId)
    {
        var order = await _repository.GetAssignedOrderDetailAsync(agentId, orderId);
        return order == null
            ? ServiceResult<AgentOrderDetailDto>.Fail(
                AppMessages.OrderNotFound,
                AppConstants.ErrorCodes.NotFound,
                404)
            : ServiceResult<AgentOrderDetailDto>.Ok(order);
    }
}
