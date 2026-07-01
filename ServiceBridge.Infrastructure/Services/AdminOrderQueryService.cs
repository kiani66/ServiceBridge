using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class AdminOrderQueryService : IAdminOrderQueryService
{
    private readonly IAdminOrderQueryRepository _repository;

    public AdminOrderQueryService(IAdminOrderQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<PagedResultDto<AdminOrderListItemDto>>>
        GetOrdersAsync(
            int page,
            int pageSize,
            OrderStatus? status,
            string? search)
    {
        page = Math.Max(page, 1);
        pageSize = NormalizePageSize(pageSize);

        var orders = await _repository.GetOrdersAsync(
            page,
            pageSize,
            status,
            search);
        return ServiceResult<PagedResultDto<AdminOrderListItemDto>>.Ok(orders);
    }

    public async Task<ServiceResult<PagedResultDto<AdminAssignableOrderDto>>>
        GetPaidUnassignedOrdersAsync(int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = NormalizePageSize(pageSize);

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

    public async Task<ServiceResult<AdminOrderDetailDto>> GetOrderDetailAsync(
        Guid adminId,
        Guid orderId)
    {
        var order = await _repository.GetOrderDetailAsync(adminId, orderId);
        if (order == null)
        {
            return ServiceResult<AdminOrderDetailDto>.Fail(
                AppMessages.OrderNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        return ServiceResult<AdminOrderDetailDto>.Ok(order);
    }

    public async Task<ServiceResult<PagedResultDto<AdminPaymentListItemDto>>>
        GetPaymentsAsync(
            int page,
            int pageSize,
            PaymentStatus? status,
            string? search)
    {
        page = Math.Max(page, 1);
        pageSize = NormalizePageSize(pageSize);

        var payments = await _repository.GetPaymentsAsync(
            page,
            pageSize,
            status,
            search);
        return ServiceResult<PagedResultDto<AdminPaymentListItemDto>>.Ok(payments);
    }

    private static int NormalizePageSize(int pageSize)
    {
        return pageSize <= 0
            ? IAdminOrderQueryService.DefaultPageSize
            : Math.Min(pageSize, IAdminOrderQueryService.MaximumPageSize);
    }
}
