using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminUserManagementService
{
    Task<ServiceResult<AdminUserListItemDto>> CreateAgentAsync(
        Guid adminId,
        CreateAgentRequest request);

    Task<ServiceResult<AdminUserListItemDto>> SetStatusAsync(
        Guid adminId,
        Guid userId,
        SetUserStatusRequest request);
}
