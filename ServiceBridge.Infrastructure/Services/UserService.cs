using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;
using Microsoft.Extensions.Logging;


namespace ServiceBridge.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<UserProfileResponse>> GetProfileAsync(Guid userId)
    {
        _logger.LogInformation("Getting profile for user: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Profile not found for user: {UserId}", userId);
            return ServiceResult<UserProfileResponse>.Fail(
                AppMessages.UserNotFound,
                AppConstants.ErrorCodes.UserNotFound,
                404
            );
        }

        var profile = MapToProfileResponse(user);
        return ServiceResult<UserProfileResponse>.Ok(profile, AppMessages.ProfileRetrieved);
    }

    public async Task<ServiceResult<UserProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        _logger.LogInformation("Updating profile for user: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Update profile failed - user not found: {UserId}", userId);
            return ServiceResult<UserProfileResponse>.Fail(
                AppMessages.UserNotFound,
                AppConstants.ErrorCodes.UserNotFound,
                404
            );
        }

        user.FullName = request.FullName;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Profile updated successfully for user: {UserId}", userId);
        var profile = MapToProfileResponse(user);
        return ServiceResult<UserProfileResponse>.Ok(profile, "Profile updated successfully.");
    }

    public async Task<ServiceResult<UserProfileResponse>> UpdateProfilePhotoAsync(Guid userId, UpdateProfilePhotoRequest request)
    {
        _logger.LogInformation("Updating profile photo for user: {UserId}", userId);

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Update photo failed - user not found: {UserId}", userId);
            return ServiceResult<UserProfileResponse>.Fail(
                AppMessages.UserNotFound,
                AppConstants.ErrorCodes.UserNotFound,
                404
            );
        }

        user.ProfileImageUrl = request.ProfileImageUrl;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Profile photo updated successfully for user: {UserId}", userId);
        var profile = MapToProfileResponse(user);
        return ServiceResult<UserProfileResponse>.Ok(profile, "Profile photo updated successfully.");
    }

    private static UserProfileResponse MapToProfileResponse(Domain.Entities.User user)
    {
        return new UserProfileResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            ProfileImageUrl = user.ProfileImageUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
