using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Application.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public PasswordResetService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ServiceResult<bool>> ResetByAdminAsync(
        Guid userId,
        AdminResetPasswordRequest request)
    {
        if (!PasswordPolicy.IsValid(request.NewPassword))
        {
            return InvalidPassword();
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<bool>.Fail(
                AppMessages.UserNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        await _userRepository.UpdatePasswordAsync(
            user,
            _passwordHasher.Hash(request.NewPassword));

        return ServiceResult<bool>.Ok(
            true,
            AppMessages.PasswordResetSuccessfully);
    }

    public async Task<ServiceResult<bool>> ChangeOwnPasswordAsync(
        Guid userId,
        AuthChangePasswordRequest request)
    {
        if (!PasswordPolicy.IsValid(request.NewPassword))
        {
            return InvalidPassword();
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ServiceResult<bool>.Fail(
                AppMessages.UserNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (string.IsNullOrEmpty(request.CurrentPassword) ||
            !_passwordHasher.Verify(
                request.CurrentPassword,
                user.PasswordHash))
        {
            return ServiceResult<bool>.Fail(
                AppMessages.CurrentPasswordIncorrect,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        await _userRepository.UpdatePasswordAsync(
            user,
            _passwordHasher.Hash(request.NewPassword));

        return ServiceResult<bool>.Ok(
            true,
            AppMessages.PasswordChangedSuccessfully);
    }

    private static ServiceResult<bool> InvalidPassword()
    {
        return ServiceResult<bool>.Fail(
            AppMessages.InvalidPasswordPolicy,
            AppConstants.ErrorCodes.Conflict,
            409);
    }
}
