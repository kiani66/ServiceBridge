using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Infrastructure.Services;

public class AdminUserQueryService : IAdminUserQueryService
{
    private readonly IAdminUserQueryRepository _repository;

    public AdminUserQueryService(IAdminUserQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<PagedResultDto<AdminUserListItemDto>>>
        GetUsersAsync(int page, int pageSize, string? search)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0
            ? IAdminUserQueryService.DefaultPageSize
            : Math.Min(pageSize, IAdminUserQueryService.MaximumPageSize);

        var result = await _repository.GetUsersAsync(page, pageSize, search);
        return ServiceResult<PagedResultDto<AdminUserListItemDto>>.Ok(result);
    }
}
