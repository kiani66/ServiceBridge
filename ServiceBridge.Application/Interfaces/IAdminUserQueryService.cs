using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminUserQueryService
{
    const int DefaultPageSize = 20;
    const int MaximumPageSize = 100;

    Task<ServiceResult<PagedResultDto<AdminUserListItemDto>>> GetUsersAsync(
        int page,
        int pageSize,
        string? search);
}
