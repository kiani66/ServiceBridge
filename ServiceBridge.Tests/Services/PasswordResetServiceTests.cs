using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class PasswordResetServiceTests
{
    [Fact]
    public async Task AdminReset_WhenUserExists_UpdatesPasswordOnly()
    {
        var repository = new FakeUserRepository();
        var user = CreateUser();
        var originalIsActive = user.IsActive;
        var originalFullName = user.FullName;
        repository.Seed(user);
        var service = CreateService(repository);

        var result = await service.ResetByAdminAsync(
            user.Id,
            new AdminResetPasswordRequest
            {
                NewPassword = "AdminReset1!"
            });

        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.True(
            BCrypt.Net.BCrypt.Verify("AdminReset1!", user.PasswordHash));
        Assert.Equal(originalIsActive, user.IsActive);
        Assert.Equal(originalFullName, user.FullName);
        Assert.Equal(1, repository.UpdatePasswordCallCount);
    }

    [Fact]
    public async Task AdminReset_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var result = await service.ResetByAdminAsync(
            Guid.NewGuid(),
            new AdminResetPasswordRequest
            {
                NewPassword = "AdminReset1!"
            });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task AdminReset_WhenPasswordIsInvalid_ReturnsConflict()
    {
        var repository = new FakeUserRepository();
        var user = CreateUser();
        repository.Seed(user);
        var service = CreateService(repository);

        var result = await service.ResetByAdminAsync(
            user.Id,
            new AdminResetPasswordRequest { NewPassword = "short" });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
        Assert.Equal(0, repository.UpdatePasswordCallCount);
    }

    [Fact]
    public async Task ChangeOwnPassword_WhenCurrentPasswordIsCorrect_UpdatesHash()
    {
        var repository = new FakeUserRepository();
        var user = CreateUser();
        repository.Seed(user);
        var service = CreateService(repository);

        var result = await service.ChangeOwnPasswordAsync(
            user.Id,
            new AuthChangePasswordRequest
            {
                CurrentPassword = "CurrentPassword1!",
                NewPassword = "NewPassword1!"
            });

        Assert.True(result.Success);
        Assert.True(
            BCrypt.Net.BCrypt.Verify("NewPassword1!", user.PasswordHash));
        Assert.Equal(1, repository.UpdatePasswordCallCount);
    }

    [Fact]
    public async Task ChangeOwnPassword_WhenCurrentPasswordIsWrong_ReturnsConflict()
    {
        var repository = new FakeUserRepository();
        var user = CreateUser();
        repository.Seed(user);
        var service = CreateService(repository);

        var result = await service.ChangeOwnPasswordAsync(
            user.Id,
            new AuthChangePasswordRequest
            {
                CurrentPassword = "WrongPassword1!",
                NewPassword = "NewPassword1!"
            });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
        Assert.Equal(0, repository.UpdatePasswordCallCount);
    }

    [Fact]
    public async Task ChangeOwnPassword_WhenNewPasswordIsInvalid_ReturnsConflict()
    {
        var repository = new FakeUserRepository();
        var user = CreateUser();
        repository.Seed(user);
        var service = CreateService(repository);

        var result = await service.ChangeOwnPasswordAsync(
            user.Id,
            new AuthChangePasswordRequest
            {
                CurrentPassword = "CurrentPassword1!",
                NewPassword = "short"
            });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
        Assert.Equal(0, repository.UpdatePasswordCallCount);
    }

    private static PasswordResetService CreateService(
        FakeUserRepository repository)
    {
        return new PasswordResetService(
            repository,
            new BcryptPasswordHasher());
    }

    private static User CreateUser()
    {
        return new User
        {
            FullName = "Test User",
            Email = "user@paybridge.test",
            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword("CurrentPassword1!"),
            IsActive = true
        };
    }
}
