using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;

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
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        var result = await _userService.GetProfileAsync(userId);
        return ToActionResult(result);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        var result = await _userService.UpdateProfileAsync(userId, request);
        return ToActionResult(result);
    }

    [HttpPut("photo")]
    public async Task<IActionResult> UpdateProfilePhoto([FromBody] UpdateProfilePhotoRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        var result = await _userService.UpdateProfilePhotoAsync(userId, request);
        return ToActionResult(result);
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        var result = await _passwordResetService.ChangeOwnPasswordAsync(
            userId,
            new AuthChangePasswordRequest
            {
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword
            });
        return ToActionResult(result);
    }
}
