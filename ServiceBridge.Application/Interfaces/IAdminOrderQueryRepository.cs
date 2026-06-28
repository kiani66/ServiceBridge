using ServiceBridge.Application.DTOs;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminOrderQueryRepository
{
    Task<PagedResultDto<AdminAssignableOrderDto>> GetPaidUnassignedOrdersAsync(
        int page,
        int pageSize);
    Task<IReadOnlyCollection<AgentSummaryDto>> GetActiveAgentsAsync();
    Task<AdminOrderDashboardSummaryDto> GetDashboardSummaryAsync();
}
