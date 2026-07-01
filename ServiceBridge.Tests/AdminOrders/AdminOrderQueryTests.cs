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

    [Fact]
    public async Task AuditEvents_BoundsPagingBeforeRepositoryCall()
    {
        var repository = new FakeAdminOrderQueryRepository();
        var service = new AdminOrderQueryService(repository);

        var result = await service.GetAuditEventsAsync(new AdminAuditQueryDto
        {
            Page = 0,
            PageSize = 1000
        });

        Assert.True(result.Success);
        Assert.NotNull(repository.LastAuditQuery);
        Assert.Equal(1, repository.LastAuditQuery.Page);
        Assert.Equal(
            IAdminOrderQueryService.MaximumPageSize,
            repository.LastAuditQuery.PageSize);
    }

    [Fact]
    public async Task AuditEvents_RejectsInvalidDateRange()
    {
        var repository = new FakeAdminOrderQueryRepository();
        var service = new AdminOrderQueryService(repository);

        var result = await service.GetAuditEventsAsync(new AdminAuditQueryDto
        {
            FromUtc = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Null(repository.LastAuditQuery);
    }

    [Fact]
    public async Task OrderTimeline_ReturnsNotFoundWhenOrderIsMissing()
    {
        var repository = new FakeAdminOrderQueryRepository();
        var service = new AdminOrderQueryService(repository);

        var result = await service.GetOrderTimelineAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    private sealed class FakeAdminOrderQueryRepository
        : IAdminOrderQueryRepository
    {
        public int? LastPage { get; private set; }
        public int? LastPageSize { get; private set; }
        public AdminAuditQueryDto? LastAuditQuery { get; private set; }

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

        public Task<PagedResultDto<AdminAuditEventDto>> GetAuditEventsAsync(
            AdminAuditQueryDto query)
        {
            LastAuditQuery = query;
            return Task.FromResult(new PagedResultDto<AdminAuditEventDto>
            {
                Page = query.Page,
                PageSize = query.PageSize
            });
        }

        public Task<IReadOnlyCollection<AdminOrderTimelineEventDto>?>
            GetOrderTimelineAsync(Guid orderId)
        {
            return Task.FromResult<IReadOnlyCollection<AdminOrderTimelineEventDto>?>(
                null);
        }
    }
}
