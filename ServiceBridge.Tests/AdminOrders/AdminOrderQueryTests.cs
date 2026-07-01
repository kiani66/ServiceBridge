using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using Xunit;

namespace ServiceBridge.Tests.AdminOrders;

public class AdminOrderQueryTests
{
    [Fact]
    public async Task PaidUnassignedOrders_BoundsPagingBeforeRepositoryCall()
    {
        var repository = new FakeAdminOrderQueryRepository();
        var service = new AdminOrderQueryService(repository);

        var result = await service.GetPaidUnassignedOrdersAsync(0, 1000);

        Assert.True(result.Success);
        Assert.Equal(1, repository.LastPage);
        Assert.Equal(
            IAdminOrderQueryService.MaximumPageSize,
            repository.LastPageSize);
    }

    private sealed class FakeAdminOrderQueryRepository
        : IAdminOrderQueryRepository
    {
        public int? LastPage { get; private set; }
        public int? LastPageSize { get; private set; }

        public Task<PagedResultDto<AdminOrderListItemDto>> GetOrdersAsync(
            int page,
            int pageSize,
            OrderStatus? status,
            string? search)
        {
            return Task.FromResult(new PagedResultDto<AdminOrderListItemDto>
            {
                Page = page,
                PageSize = pageSize
            });
        }

        public Task<PagedResultDto<AdminAssignableOrderDto>>
            GetPaidUnassignedOrdersAsync(int page, int pageSize)
        {
            LastPage = page;
            LastPageSize = pageSize;
            return Task.FromResult(new PagedResultDto<AdminAssignableOrderDto>
            {
                Page = page,
                PageSize = pageSize
            });
        }

        public Task<IReadOnlyCollection<AgentSummaryDto>> GetActiveAgentsAsync()
        {
            return Task.FromResult<IReadOnlyCollection<AgentSummaryDto>>(
                Array.Empty<AgentSummaryDto>());
        }

        public Task<AdminOrderDashboardSummaryDto> GetDashboardSummaryAsync()
        {
            return Task.FromResult(new AdminOrderDashboardSummaryDto());
        }

        public Task<AdminOrderDetailDto?> GetOrderDetailAsync(
            Guid adminId,
            Guid orderId)
        {
            return Task.FromResult<AdminOrderDetailDto?>(null);
        }

        public Task<PagedResultDto<AdminPaymentListItemDto>> GetPaymentsAsync(
            int page,
            int pageSize,
            PaymentStatus? status,
            string? search)
        {
            return Task.FromResult(new PagedResultDto<AdminPaymentListItemDto>
            {
                Page = page,
                PageSize = pageSize
            });
        }
    }
}
