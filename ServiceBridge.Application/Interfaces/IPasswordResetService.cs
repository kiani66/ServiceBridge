using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IPasswordResetService
{
    Task<ServiceResult<bool>> ResetByAdminAsync(
        Guid userId,
        AdminResetPasswordRequest request);

    Task<ServiceResult<bool>> ChangeOwnPasswordAsync(
        Guid userId,
        AuthChangePasswordRequest request);
}
