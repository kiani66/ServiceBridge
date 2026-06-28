using Microsoft.Extensions.Logging.Abstractions;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task GetProfileAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository);

        var result = await service.GetProfileAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.UserNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenUserExists_UpdatesFullName()
    {
        var repository = new FakeUserRepository();
        var user = CreateUser();
        repository.Seed(user);
        var service = CreateService(repository);

        var result = await service.UpdateProfileAsync(user.Id, new UpdateProfileRequest
        {
            FullName = "Updated Name"
        });

        Assert.True(result.Success);
        Assert.Equal("Updated Name", result.Data?.FullName);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    private static UserService CreateService(FakeUserRepository repository)
    {
        return new UserService(repository, NullLogger<UserService>.Instance);
    }

    private static User CreateUser()
    {
        return new User
        {
            FullName = "Test User",
            Email = "user@servicebridge.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CurrentPassword1!"),
            IsActive = true
        };
    }
}
