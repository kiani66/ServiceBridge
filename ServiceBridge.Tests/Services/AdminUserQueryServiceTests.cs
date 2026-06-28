using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class AdminUserQueryServiceTests
{
    [Fact]
    public async Task GetUsersAsync_BoundsPagingAndForwardsSearch()
    {
        var repository = new FakeAdminUserQueryRepository
        {
            Users =
            [
                new AdminUserListItemDto
                {
                    UserId = Guid.NewGuid(),
                    FullName = "Test User"
                }
            ]
        };
        var service = new AdminUserQueryService(repository);

        var result = await service.GetUsersAsync(0, 1000, "test");

        Assert.True(result.Success);
        Assert.Single(result.Data!.Items);
        Assert.Equal(1, repository.LastPage);
        Assert.Equal(
            IAdminUserQueryService.MaximumPageSize,
            repository.LastPageSize);
        Assert.Equal("test", repository.LastSearch);
    }
}
