using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.Results;

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
}
