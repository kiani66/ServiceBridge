using Microsoft.Extensions.Logging.Abstractions;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ReturnsConflict()
    {
        var repository = new FakeUserRepository();
        repository.Seed(CreateUser("user@servicebridge.test", "Password123!"));
        var jwtService = new FakeJwtTokenService();
        var service = CreateService(repository, jwtService);

        var result = await service.RegisterAsync(new UserRegisterRequest
        {
            FullName = "Existing User",
            Email = "user@servicebridge.test",
            Password = "Password123!"
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.DuplicateEmail, result.ErrorCode);
    }

    [Fact]
    public async Task RegisterAsync_WhenPasswordViolatesPolicy_ReturnsConflict()
    {
        var repository = new FakeUserRepository();
        var jwtService = new FakeJwtTokenService();
        var service = CreateService(repository, jwtService);

        var result = await service.RegisterAsync(new UserRegisterRequest
        {
            FullName = "New User",
            Email = "new@servicebridge.test",
            Password = "short"
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ReturnsInvalidCredentials()
    {
        var repository = new FakeUserRepository();
        var jwtService = new FakeJwtTokenService();
        var service = CreateService(repository, jwtService);

        var result = await service.LoginAsync(new UserLoginRequest
        {
            Email = "missing@servicebridge.test",
            Password = "Password123!"
        });

        Assert.False(result.Success);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.InvalidCredentials, result.ErrorCode);
        Assert.Equal(0, jwtService.GenerateTokenCallCount);
    }

    [Fact]
    public async Task LoginAsync_WhenInactiveUserHasWrongPassword_ReturnsInvalidCredentials()
    {
        var repository = new FakeUserRepository();
        var user = CreateUser("inactive@servicebridge.test", "CorrectPassword1!");
        user.IsActive = false;
        repository.Seed(user);
        var jwtService = new FakeJwtTokenService();
        var service = CreateService(repository, jwtService);

        var result = await service.LoginAsync(new UserLoginRequest
        {
            Email = user.Email,
            Password = "WrongPassword1!"
        });

        Assert.False(result.Success);
        Assert.Equal(AppConstants.ErrorCodes.InvalidCredentials, result.ErrorCode);
    }

    [Fact]
    public async Task LoginAsync_WhenInactiveUserHasCorrectPassword_ReturnsUserInactive()
    {
        var repository = new FakeUserRepository();
        var user = CreateUser("inactive@servicebridge.test", "CorrectPassword1!");
        user.IsActive = false;
        repository.Seed(user);
        var jwtService = new FakeJwtTokenService();
        var service = CreateService(repository, jwtService);

        var result = await service.LoginAsync(new UserLoginRequest
        {
            Email = user.Email,
            Password = "CorrectPassword1!"
        });

        Assert.False(result.Success);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.UserInactive, result.ErrorCode);
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsTokenAndUserData()
    {
        var repository = new FakeUserRepository();
        var user = CreateUser("active@servicebridge.test", "CorrectPassword1!");
        repository.Seed(user);
        var jwtService = new FakeJwtTokenService();
        var service = CreateService(repository, jwtService);

        var result = await service.LoginAsync(new UserLoginRequest
        {
            Email = user.Email,
            Password = "CorrectPassword1!"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(FakeJwtTokenService.Token, result.Data.AccessToken);
        Assert.Equal(FakeJwtTokenService.ExpirationUtc, result.Data.ExpiresAtUtc);
        Assert.Equal(user.Role.ToString(), result.Data.Role);
        Assert.Equal(1, jwtService.GenerateTokenCallCount);
    }

    private static AuthService CreateService(
        FakeUserRepository repository,
        FakeJwtTokenService jwtService)
    {
        return new AuthService(
            repository,
            jwtService,
            new BcryptPasswordHasher(),
            NullLogger<AuthService>.Instance);
    }

    private static User CreateUser(string email, string password)
    {
        return new User
        {
            FullName = "Test User",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.User,
            IsActive = true
        };
    }
}
