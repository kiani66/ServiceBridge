using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Api.Controllers;

[Authorize]
[Route("api/user")]
public class UserController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IPasswordResetService _passwordResetService;

    public UserController(
        IUserService userService,
        IPasswordResetService passwordResetService)
    {
        _userService = userService;
        _passwordResetService = passwordResetService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
        {
            return StatusCode(401, ServiceBridge.Application.Results.ServiceResult.Fail(
                AppMessages.UnauthorizedAccess,
                AppConstants.ErrorCodes.Unauthorized,
                401
            ));
        }

        var result = await _userService.GetProfileAsync(userId.Value);
        return ToActionResult(result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
        {
            return StatusCode(401, ServiceBridge.Application.Results.ServiceResult.Fail(
                AppMessages.UnauthorizedAccess,
                AppConstants.ErrorCodes.Unauthorized,
                401
            ));
        }

        var result = await _userService.UpdateProfileAsync(userId.Value, request);
        return ToActionResult(result);
    }

    [HttpPut("photo")]
    public async Task<IActionResult> UpdateProfilePhoto([FromBody] UpdateProfilePhotoRequest request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
        {
            return StatusCode(401, ServiceBridge.Application.Results.ServiceResult.Fail(
                AppMessages.UnauthorizedAccess,
                AppConstants.ErrorCodes.Unauthorized,
                401
            ));
        }

        var result = await _userService.UpdateProfilePhotoAsync(userId.Value, request);
        return ToActionResult(result);
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null)
        {
            return StatusCode(401, ServiceBridge.Application.Results.ServiceResult.Fail(
                AppMessages.UnauthorizedAccess,
                AppConstants.ErrorCodes.Unauthorized,
                401
            ));
        }

        var result = await _passwordResetService.ChangeOwnPasswordAsync(
            userId.Value,
            new AuthChangePasswordRequest
            {
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            });
        return ToActionResult(result);
    }

    private Guid? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }
}
