using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminOrderQueryService
{
    const int DefaultPageSize = 20;
    const int MaximumPageSize = 100;

    Task<ServiceResult<PagedResultDto<AdminOrderListItemDto>>> GetOrdersAsync(
        int page,
        int pageSize,
        OrderStatus? status,
        string? search,
        Guid? assignedAgentId);

    Task<ServiceResult<PagedResultDto<AdminAssignableOrderDto>>>
        GetPaidUnassignedOrdersAsync(int page, int pageSize);

    Task<ServiceResult<PagedResultDto<AdminPendingInventoryFulfillmentDto>>>
        GetPendingInventoryFulfillmentAsync(int page, int pageSize);

    Task<ServiceResult<IReadOnlyCollection<AgentSummaryDto>>> GetActiveAgentsAsync();
    Task<ServiceResult<AdminOrderDashboardSummaryDto>> GetDashboardSummaryAsync();
    Task<ServiceResult<AdminOrderDetailDto>> GetOrderDetailAsync(
        Guid adminId,
        Guid orderId);
    Task<ServiceResult<AdminOrderCredentialsDto>> RevealOrderCredentialsAsync(
        Guid adminId,
        Guid orderId);
    Task<ServiceResult<PagedResultDto<AdminPaymentListItemDto>>> GetPaymentsAsync(
        int page,
        int pageSize,
        PaymentStatus? status,
        string? search);
    Task<ServiceResult<PagedResultDto<AdminAuditEventDto>>> GetAuditEventsAsync(
        AdminAuditQueryDto query);
    Task<ServiceResult<IReadOnlyCollection<AdminOrderTimelineEventDto>>>
        GetOrderTimelineAsync(Guid orderId);
}
