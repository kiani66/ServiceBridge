using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IUserService
{
    Task<ServiceResult<UserProfileResponse>> GetProfileAsync(Guid userId);
    Task<ServiceResult<UserProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<ServiceResult<UserProfileResponse>> UpdateProfilePhotoAsync(Guid userId, UpdateProfilePhotoRequest request);
}
