using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.Admin)]
[Route("api/admin/users")]
public class AdminUsersController : BaseApiController
{
    private readonly IPasswordResetService _passwordResetService;
    private readonly IAdminUserQueryService _queryService;
    private readonly IAdminUserManagementService _managementService;

    public AdminUsersController(
        IPasswordResetService passwordResetService,
        IAdminUserQueryService queryService,
        IAdminUserManagementService managementService)
    {
        _passwordResetService = passwordResetService;
        _queryService = queryService;
        _managementService = managementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = IAdminUserQueryService.DefaultPageSize,
        [FromQuery] string? search = null)
    {
        var result = await _queryService.GetUsersAsync(page, pageSize, search);
        return ToActionResult(result);
    }

    [HttpPost("agents")]
    public async Task<IActionResult> CreateAgent(
        [FromBody] CreateAgentRequest request)
    {
        if (!TryGetUserId(out var adminId))
        {
            return UnauthorizedServiceResult();
        }

        var result = await _managementService.CreateAgentAsync(adminId, request);
        return ToActionResult(result);
    }

    [HttpPatch("{userId:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid userId,
        [FromBody] SetUserStatusRequest request)
    {
        if (!TryGetUserId(out var adminId))
        {
            return UnauthorizedServiceResult();
        }

        var result = await _managementService.SetStatusAsync(
            adminId,
            userId,
            request);
        return ToActionResult(result);
    }

    [HttpPost("{userId:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid userId,
        [FromBody] AdminResetPasswordRequest request)
    {
        var result = await _passwordResetService.ResetByAdminAsync(
            userId,
            request);
        return ToActionResult(result);
    }
}
