using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminOrderQueryRepository
{
    Task<PagedResultDto<AdminOrderListItemDto>> GetOrdersAsync(
        int page,
        int pageSize,
        OrderStatus? status,
        string? search);
    Task<PagedResultDto<AdminAssignableOrderDto>> GetPaidUnassignedOrdersAsync(
        int page,
        int pageSize);
    Task<IReadOnlyCollection<AgentSummaryDto>> GetActiveAgentsAsync();
    Task<AdminOrderDashboardSummaryDto> GetDashboardSummaryAsync();
    Task<AdminOrderDetailDto?> GetOrderDetailAsync(Guid adminId, Guid orderId);
    Task<PagedResultDto<AdminPaymentListItemDto>> GetPaymentsAsync(
        int page,
        int pageSize,
        PaymentStatus? status,
        string? search);
}
