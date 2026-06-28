using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Api.Controllers;

[Route("api/auth")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IPasswordResetService _passwordResetService;

    public AuthController(
        IAuthService authService,
        IPasswordResetService passwordResetService)
    {
        _authService = authService;
        _passwordResetService = passwordResetService;
    }

    [AllowAnonymous]
    [HttpPost("user/register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("user/login")]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] AuthChangePasswordRequest request)
    {
        if (!Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
        {
            return ToActionResult(ServiceResult<bool>.Fail(
                AppMessages.UnauthorizedAccess,
                AppConstants.ErrorCodes.Unauthorized,
                401));
        }

        var result = await _passwordResetService.ChangeOwnPasswordAsync(
            userId,
            request);
        return ToActionResult(result);
    }
}
