using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class AdminUserManagementServiceTests
{
    [Fact]
    public async Task CreateAgentAsync_WhenValid_CreatesActiveAgent()
    {
        var repository = new FakeAdminUserCommandRepository();
        var auditLogWriter = new FakeAuditLogWriter();
        var service = CreateService(repository, auditLogWriter);
        var adminId = Guid.NewGuid();

        var result = await service.CreateAgentAsync(adminId, new CreateAgentRequest
        {
            FullName = "Agent One",
            Email = "AGENT@SERVICEBRIDGE.TEST",
            Password = "AgentPassword1!"
        });

        Assert.True(result.Success);
        Assert.Equal(201, result.StatusCode);
        var agent = Assert.Single(repository.Users);
        Assert.Equal(UserRole.Agent, agent.Role);
        Assert.True(agent.IsActive);
        Assert.Equal("agent@servicebridge.test", agent.Email);
        Assert.True(
            BCrypt.Net.BCrypt.Verify("AgentPassword1!", agent.PasswordHash));
        Assert.Equal(1, repository.SaveCount);
        var audit = Assert.Single(auditLogWriter.Entries);
        Assert.Equal(Domain.Enums.AuditActionType.AgentCreated, audit.ActionType);
        Assert.Equal(adminId, audit.ActorId);
        Assert.Equal(agent.Id, audit.EntityId);
    }

    [Fact]
    public async Task CreateAgentAsync_WhenEmailExists_ReturnsConflict()
    {
        var repository = new FakeAdminUserCommandRepository();
        repository.Seed(CreateUser(UserRole.User));
        var service = CreateService(repository);

        var result = await service.CreateAgentAsync(Guid.NewGuid(), new CreateAgentRequest
        {
            FullName = "Agent One",
            Email = "user@servicebridge.test",
            Password = "AgentPassword1!"
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.DuplicateEmail, result.ErrorCode);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task SetStatusAsync_WhenUserExists_UpdatesStatus()
    {
        var repository = new FakeAdminUserCommandRepository();
        var user = CreateUser(UserRole.User);
        repository.Seed(user);
        var auditLogWriter = new FakeAuditLogWriter();
        var service = CreateService(repository, auditLogWriter);
        var adminId = Guid.NewGuid();

        var result = await service.SetStatusAsync(
            adminId,
            user.Id,
            new SetUserStatusRequest { IsActive = false });

        Assert.True(result.Success);
        Assert.False(user.IsActive);
        Assert.NotNull(user.UpdatedAt);
        Assert.Equal(1, repository.SaveCount);
        var audit = Assert.Single(auditLogWriter.Entries);
        Assert.Equal(Domain.Enums.AuditActionType.UserDeactivated, audit.ActionType);
        Assert.Equal(adminId, audit.ActorId);
        Assert.Equal(user.Id, audit.EntityId);
    }

    [Fact]
    public async Task SetStatusAsync_WhenTargetIsAdmin_ReturnsConflict()
    {
        var repository = new FakeAdminUserCommandRepository();
        var admin = CreateUser(UserRole.Admin);
        repository.Seed(admin);
        var service = CreateService(repository);

        var result = await service.SetStatusAsync(
            Guid.NewGuid(),
            admin.Id,
            new SetUserStatusRequest { IsActive = false });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.True(admin.IsActive);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task SetStatusAsync_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var service = CreateService(new FakeAdminUserCommandRepository());

        var result = await service.SetStatusAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new SetUserStatusRequest { IsActive = false });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    private static AdminUserManagementService CreateService(
        FakeAdminUserCommandRepository repository,
        FakeAuditLogWriter? auditLogWriter = null)
    {
        return new AdminUserManagementService(
            repository,
            new BcryptPasswordHasher(),
            auditLogWriter ?? new FakeAuditLogWriter());
    }

    private static User CreateUser(UserRole role)
    {
        return new User
        {
            FullName = "Test User",
            Email = "user@servicebridge.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
            Role = role,
            IsActive = true
        };
    }
}
