using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminOrderQueryService
{
    const int DefaultPageSize = 20;
    const int MaximumPageSize = 100;

    Task<ServiceResult<PagedResultDto<AdminAssignableOrderDto>>>
        GetPaidUnassignedOrdersAsync(int page, int pageSize);

    Task<ServiceResult<IReadOnlyCollection<AgentSummaryDto>>> GetActiveAgentsAsync();
    Task<ServiceResult<AdminOrderDashboardSummaryDto>> GetDashboardSummaryAsync();
}
