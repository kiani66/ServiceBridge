using ServiceBridge.Application.Audit;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using Xunit;

namespace ServiceBridge.Tests.Audit;

public class GetAuditLogsQueryTests
{
    [Fact]
    public async Task ExecuteAsync_NormalizesPagingAndReturnsProjection()
    {
        var repository = new FakeAuditLogQueryRepository();
        var query = new GetAuditLogsQuery(repository);

        var result = await query.ExecuteAsync(
            page: 0,
            pageSize: 250,
            actorId: null,
            entityName: null,
            entityId: null,
            actionType: null);

        Assert.True(result.Success);
        Assert.Equal(1, repository.Page);
        Assert.Equal(GetAuditLogsQuery.MaximumPageSize, repository.PageSize);
        Assert.Equal(2, result.Data!.TotalCount);
        Assert.Equal(2, result.Data.Items.Count);
    }

    [Fact]
    public void AuditLogListItemDto_DoesNotExposeBeforeOrAfterJson()
    {
        var properties = typeof(AuditLogListItemDto)
            .GetProperties()
            .Select(x => x.Name)
            .ToArray();

        Assert.DoesNotContain("BeforeJson", properties);
        Assert.DoesNotContain("AfterJson", properties);
        Assert.DoesNotContain("Before", properties);
        Assert.DoesNotContain("After", properties);
    }

    private sealed class FakeAuditLogQueryRepository : IAuditLogQueryRepository
    {
        public int Page { get; private set; }
        public int PageSize { get; private set; }

        public Task<PagedResultDto<AuditLogListItemDto>> GetAuditLogsAsync(
            int page,
            int pageSize,
            Guid? actorId,
            string? entityName,
            Guid? entityId,
            AuditActionType? actionType,
            CancellationToken cancellationToken = default)
        {
            Page = page;
            PageSize = pageSize;

            return Task.FromResult(new PagedResultDto<AuditLogListItemDto>
            {
                Items =
                [
                    new AuditLogListItemDto { Id = Guid.NewGuid() },
                    new AuditLogListItemDto { Id = Guid.NewGuid() }
                ],
                Page = page,
                PageSize = pageSize,
                TotalCount = 2
            });
        }
    }
}
