using ServiceBridge.Application.Audit;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Application.Services;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;
using Microsoft.Extensions.Logging;

namespace ServiceBridge.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthService> _logger;
    private readonly IAuditLogWriter _auditLogWriter;

    public AuthService(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        ILogger<AuthService> logger,
        IAuditLogWriter auditLogWriter)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<ServiceResult> RegisterAsync(UserRegisterRequest request)
    {
        _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

        var emailExists = await _userRepository.EmailExistsAsync(request.Email);
        if (emailExists)
        {
            _logger.LogWarning("Registration failed - duplicate email: {Email}", request.Email);
            return ServiceResult.Fail(
                AppMessages.DuplicateEmail,
                AppConstants.ErrorCodes.DuplicateEmail,
                409
            );
        }

        if (!PasswordPolicy.IsValid(request.Password))
        {
            return ServiceResult.Fail(
                AppMessages.InvalidPasswordPolicy,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.User,
            IsActive = true
        };

        await _userRepository.AddAsync(user);
        await _auditLogWriter.WriteAsync(new AuditLogEntry
        {
            ActorId = user.Id,
            ActorRole = nameof(UserRole.User),
            ActionType = AuditActionType.UserCreated,
            EntityName = nameof(User),
            EntityId = user.Id,
            After = new
            {
                user.Id,
                user.Email,
                Role = user.Role.ToString(),
                user.IsActive
            },
            Description = $"User account {user.Email} was created."
        });

        _logger.LogInformation("User registered successfully: {Email}", request.Email);
        return ServiceResult.Created(AppMessages.RegistrationSuccess);
    }

    public async Task<ServiceResult<LoginResponseDto>> LoginAsync(UserLoginRequest request)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        // Step 1: Find user by email
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed - user not found: {Email}", request.Email);
            return ServiceResult<LoginResponseDto>.Fail(
                AppMessages.InvalidCredentials,
                AppConstants.ErrorCodes.InvalidCredentials,
                401
            );
        }

        // Step 2: Verify password
        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed - invalid password for: {Email}", request.Email);
            return ServiceResult<LoginResponseDto>.Fail(
                AppMessages.InvalidCredentials,
                AppConstants.ErrorCodes.InvalidCredentials,
                401
            );
        }

        // Step 3: Check IsActive (only after password verification)
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed - inactive account: {Email}", request.Email);
            return ServiceResult<LoginResponseDto>.Fail(
                AppMessages.UserInactive,
                AppConstants.ErrorCodes.UserInactive,
                401
            );
        }

        // Generate JWT token
        var token = _jwtTokenService.GenerateToken(user);
        var expiresAt = _jwtTokenService.GetExpirationUtc();

        var response = new LoginResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            AccessToken = token,
            ExpiresAtUtc = expiresAt
        };

        _logger.LogInformation("Login successful for: {Email}", request.Email);
        return ServiceResult<LoginResponseDto>.Ok(response, AppMessages.LoginSuccess);
    }
}
