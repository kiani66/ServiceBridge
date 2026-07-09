using ServiceBridge.Application.Audit;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Application.Services;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class AdminUserManagementService : IAdminUserManagementService
{
    private readonly IAdminUserCommandRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogWriter _auditLogWriter;

    public AdminUserManagementService(
        IAdminUserCommandRepository repository,
        IPasswordHasher passwordHasher,
        IAuditLogWriter auditLogWriter)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<ServiceResult<AdminUserListItemDto>> CreateAgentAsync(
        Guid adminId,
        CreateAgentRequest request)
    {
        if (!PasswordPolicy.IsValid(request.Password))
        {
            return ServiceResult<AdminUserListItemDto>.Fail(
                AppMessages.InvalidPasswordPolicy,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _repository.EmailExistsAsync(email))
        {
            return ServiceResult<AdminUserListItemDto>.Fail(
                AppMessages.DuplicateEmail,
                AppConstants.ErrorCodes.DuplicateEmail,
                409);
        }

        var agent = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Agent,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(agent);
        await _repository.SaveChangesAsync();
        await _auditLogWriter.WriteAsync(new AuditLogEntry
        {
            ActorId = adminId,
            ActorRole = nameof(UserRole.Admin),
            ActionType = AuditActionType.AgentCreated,
            EntityName = nameof(User),
            EntityId = agent.Id,
            After = new
            {
                agent.Id,
                agent.Email,
                Role = agent.Role.ToString(),
                agent.IsActive
            },
            Description = $"Agent account {agent.Email} was created."
        });

        return ServiceResult<AdminUserListItemDto>.Created(
            Map(agent),
            AppMessages.AgentCreated);
    }

    public async Task<ServiceResult<AdminUserListItemDto>> SetStatusAsync(
        Guid adminId,
        Guid userId,
        SetUserStatusRequest request)
    {
        var user = await _repository.GetUserForUpdateAsync(userId);
        if (user == null)
        {
            return ServiceResult<AdminUserListItemDto>.Fail(
                AppMessages.UserNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (user.Role == UserRole.Admin)
        {
            return ServiceResult<AdminUserListItemDto>.Fail(
                AppMessages.AdminStatusCannotBeChanged,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var previousIsActive = user.IsActive;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _repository.SaveChangesAsync();
        await _auditLogWriter.WriteAsync(new AuditLogEntry
        {
            ActorId = adminId,
            ActorRole = nameof(UserRole.Admin),
            ActionType = user.IsActive
                ? AuditActionType.UserActivated
                : AuditActionType.UserDeactivated,
            EntityName = nameof(User),
            EntityId = user.Id,
            Before = new { IsActive = previousIsActive },
            After = new { user.IsActive },
            Description = $"User account {user.Email} was {(user.IsActive ? "activated" : "deactivated")}."
        });

        return ServiceResult<AdminUserListItemDto>.Ok(
            Map(user),
            AppMessages.UserStatusUpdated);
    }

    private static AdminUserListItemDto Map(User user)
    {
        return new AdminUserListItemDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
