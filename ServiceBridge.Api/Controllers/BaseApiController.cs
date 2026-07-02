using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Api.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult ToActionResult(ServiceResult result)
    {
        return StatusCode(result.StatusCode, result);
    }

    protected IActionResult ToActionResult<T>(ServiceResult<T> result)
    {
        return StatusCode(result.StatusCode, result);
    }

    protected bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }

    protected IActionResult UnauthorizedServiceResult()
    {
        return ToActionResult(ServiceResult.Fail(
            AppMessages.UnauthorizedAccess,
            AppConstants.ErrorCodes.Unauthorized,
            401));
    }
}
