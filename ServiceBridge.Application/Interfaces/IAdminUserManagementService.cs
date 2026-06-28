using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminUserManagementService
{
    Task<ServiceResult<AdminUserListItemDto>> CreateAgentAsync(
        CreateAgentRequest request);

    Task<ServiceResult<AdminUserListItemDto>> SetStatusAsync(
        Guid userId,
        SetUserStatusRequest request);
}
