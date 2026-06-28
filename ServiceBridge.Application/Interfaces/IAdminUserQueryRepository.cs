using ServiceBridge.Application.DTOs;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminUserQueryRepository
{
    Task<PagedResultDto<AdminUserListItemDto>> GetUsersAsync(
        int page,
        int pageSize,
        string? search);
}
