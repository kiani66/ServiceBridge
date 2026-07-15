using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.AdminOrders;

public class AdminOrderQueryTests
{
    [Fact]
    public async Task PaidUnassignedOrders_BoundsPagingBeforeRepositoryCall()
    {
        var repository = new FakeAdminOrderQueryRepository();
        var service = CreateService(repository);

        var result = await service.GetPaidUnassignedOrdersAsync(0, 1000);

        Assert.True(result.Success);
        Assert.Equal(1, repository.LastPage);
        Assert.Equal(
            IAdminOrderQueryService.MaximumPageSize,
            repository.LastPageSize);
    }

    [Fact]
    public async Task PendingInventoryFulfillment_BoundsPagingBeforeRepositoryCall()
    {
        var repository = new FakeAdminOrderQueryRepository();
        var service = CreateService(repository);

        var result = await service.GetPendingInventoryFulfillmentAsync(0, 1000);

        Assert.True(result.Success);
        Assert.Equal(1, repository.LastPendingPage);
        Assert.Equal(
            IAdminOrderQueryService.MaximumPageSize,
            repository.LastPendingPageSize);
    }

    [Fact]
    public async Task AuditEvents_BoundsPagingBeforeRepositoryCall()
    {
        var repository = new FakeAdminOrderQueryRepository();
        var service = CreateService(repository);

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
        var service = CreateService(repository);

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
        var service = CreateService(repository);

        var result = await service.GetOrderTimelineAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task Orders_ForwardsSearchStatusAndAssignedAgentFilter()
    {
        var repository = new FakeAdminOrderQueryRepository();
        var service = CreateService(repository);
        var agentId = Guid.NewGuid();

        var result = await service.GetOrdersAsync(
            0,
            1000,
            OrderStatus.Processing,
            "SB-1001",
            agentId);

        Assert.True(result.Success);
        Assert.Equal(1, repository.LastOrderPage);
        Assert.Equal(
            IAdminOrderQueryService.MaximumPageSize,
            repository.LastOrderPageSize);
        Assert.Equal(OrderStatus.Processing, repository.LastOrderStatus);
        Assert.Equal("SB-1001", repository.LastOrderSearch);
        Assert.Equal(agentId, repository.LastAssignedAgentId);
    }

    [Fact]
    public async Task OrderDetail_ReturnsMaskedCredentialMetadataByDefault()
    {
        var orderId = Guid.NewGuid();
        var repository = new FakeAdminOrderQueryRepository
        {
            Detail = new AdminOrderDetailDto
            {
                OrderId = orderId,
                OrderNumber = "SB-1001",
                Credentials =
                [
                    new AgentOrderFieldDto
                    {
                        FieldKey = "password",
                        FieldLabel = "Password",
                        Value = string.Empty,
                        IsSensitive = true
                    }
                ]
            }
        };
        var service = CreateService(repository);

        var result = await service.GetOrderDetailAsync(Guid.NewGuid(), orderId);

        Assert.True(result.Success);
        var credential = Assert.Single(result.Data!.Credentials);
        Assert.Equal("Password", credential.FieldLabel);
        Assert.Equal(string.Empty, credential.Value);
    }

    [Fact]
    public async Task RevealOrderCredentials_ReturnsValuesAndWritesCredentialViewedAudit()
    {
        var adminId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var auditLogWriter = new FakeAuditLogWriter();
        var repository = new FakeAdminOrderQueryRepository
        {
            RevealedCredentials = new AdminOrderCredentialsDto
            {
                OrderId = orderId,
                OrderNumber = "SB-1001",
                Credentials =
                [
                    new AgentOrderFieldDto
                    {
                        FieldKey = "password",
                        FieldLabel = "Password",
                        Value = "super-secret",
                        IsSensitive = true
                    }
                ]
            }
        };
        var service = CreateService(repository, auditLogWriter);

        var result = await service.RevealOrderCredentialsAsync(adminId, orderId);

        Assert.True(result.Success);
        Assert.Equal("super-secret", Assert.Single(result.Data!.Credentials).Value);
        Assert.Equal(adminId, repository.LastCredentialAccessUserId);

        var auditEntry = Assert.Single(auditLogWriter.Entries);
        Assert.Equal(adminId, auditEntry.ActorId);
        Assert.Equal(AuditActionType.CredentialViewed, auditEntry.ActionType);
        Assert.Equal(orderId, auditEntry.EntityId);
        Assert.Null(auditEntry.Before);
        Assert.Null(auditEntry.After);
        Assert.DoesNotContain("super-secret", auditEntry.Description!);
    }

    [Fact]
    public void AdminOrderListItemDto_DoesNotExposeCredentialValues()
    {
        var properties = typeof(AdminOrderListItemDto)
            .GetProperties()
            .Select(x => x.Name)
            .ToArray();

        Assert.DoesNotContain("Credentials", properties);
        Assert.DoesNotContain("CredentialValues", properties);
        Assert.DoesNotContain("DecryptedCredentials", properties);
    }

    private static AdminOrderQueryService CreateService(
        FakeAdminOrderQueryRepository repository,
        FakeAuditLogWriter? auditLogWriter = null)
    {
        return new AdminOrderQueryService(
            repository,
            auditLogWriter ?? new FakeAuditLogWriter());
    }

    private sealed class FakeAdminOrderQueryRepository
        : IAdminOrderQueryRepository
    {
        public int? LastPage { get; private set; }
        public int? LastPageSize { get; private set; }
        public int? LastOrderPage { get; private set; }
        public int? LastOrderPageSize { get; private set; }
        public int? LastPendingPage { get; private set; }
        public int? LastPendingPageSize { get; private set; }
        public OrderStatus? LastOrderStatus { get; private set; }
        public string? LastOrderSearch { get; private set; }
        public Guid? LastAssignedAgentId { get; private set; }
        public AdminAuditQueryDto? LastAuditQuery { get; private set; }
        public AdminOrderDetailDto? Detail { get; init; }
        public AdminOrderCredentialsDto? RevealedCredentials { get; init; }

        public Task<PagedResultDto<AdminOrderListItemDto>> GetOrdersAsync(
            int page,
            int pageSize,
            OrderStatus? status,
            string? search,
            Guid? assignedAgentId)
        {
            LastOrderPage = page;
            LastOrderPageSize = pageSize;
            LastOrderStatus = status;
            LastOrderSearch = search;
            LastAssignedAgentId = assignedAgentId;

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

        public Task<PagedResultDto<AdminPendingInventoryFulfillmentDto>>
            GetPendingInventoryFulfillmentAsync(int page, int pageSize)
        {
            LastPendingPage = page;
            LastPendingPageSize = pageSize;
            return Task.FromResult(
                new PagedResultDto<AdminPendingInventoryFulfillmentDto>
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
            return Task.FromResult(Detail);
        }

        public Guid? LastCredentialAccessUserId { get; private set; }

        public Task<AdminOrderCredentialsDto?> GetOrderCredentialsAsync(
            Guid orderId,
            Guid accessedByUserId)
        {
            LastCredentialAccessUserId = accessedByUserId;
            return Task.FromResult(RevealedCredentials);
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
