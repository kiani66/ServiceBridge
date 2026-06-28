using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Infrastructure.Services;

public class AdminOrderQueryService : IAdminOrderQueryService
{
    private readonly IAdminOrderQueryRepository _repository;

    public AdminOrderQueryService(IAdminOrderQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<PagedResultDto<AdminAssignableOrderDto>>>
        GetPaidUnassignedOrdersAsync(int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0
            ? IAdminOrderQueryService.DefaultPageSize
            : Math.Min(pageSize, IAdminOrderQueryService.MaximumPageSize);

        var orders = await _repository.GetPaidUnassignedOrdersAsync(page, pageSize);
        return ServiceResult<PagedResultDto<AdminAssignableOrderDto>>.Ok(orders);
    }

    public async Task<ServiceResult<IReadOnlyCollection<AgentSummaryDto>>>
        GetActiveAgentsAsync()
    {
        var agents = await _repository.GetActiveAgentsAsync();
        return ServiceResult<IReadOnlyCollection<AgentSummaryDto>>.Ok(agents);
    }

    public async Task<ServiceResult<AdminOrderDashboardSummaryDto>>
        GetDashboardSummaryAsync()
    {
        var summary = await _repository.GetDashboardSummaryAsync();
        return ServiceResult<AdminOrderDashboardSummaryDto>.Ok(summary);
    }
}
