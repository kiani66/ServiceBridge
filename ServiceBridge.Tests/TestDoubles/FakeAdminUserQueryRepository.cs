using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeAdminUserQueryRepository : IAdminUserQueryRepository
{
    public int? LastPage { get; private set; }
    public int? LastPageSize { get; private set; }
    public string? LastSearch { get; private set; }
    public IReadOnlyCollection<AdminUserListItemDto> Users { get; set; } =
        Array.Empty<AdminUserListItemDto>();

    public Task<PagedResultDto<AdminUserListItemDto>> GetUsersAsync(
        int page,
        int pageSize,
        string? search)
    {
        LastPage = page;
        LastPageSize = pageSize;
        LastSearch = search;

        return Task.FromResult(new PagedResultDto<AdminUserListItemDto>
        {
            Items = Users,
            Page = page,
            PageSize = pageSize,
            TotalCount = Users.Count
        });
    }
}
