using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.Audit;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.Admin)]
[Route("api/admin/audit-logs")]
public class AdminAuditLogsController : BaseApiController
{
    private readonly GetAuditLogsQuery _getAuditLogsQuery;

    public AdminAuditLogsController(GetAuditLogsQuery getAuditLogsQuery)
    {
        _getAuditLogsQuery = getAuditLogsQuery;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetAuditLogsQuery.DefaultPageSize,
        [FromQuery] Guid? actorId = null,
        [FromQuery] string? entityName = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] AuditActionType? actionType = null,
        CancellationToken cancellationToken = default)
    {
        return ToActionResult(await _getAuditLogsQuery.ExecuteAsync(
            page,
            pageSize,
            actorId,
            entityName,
            entityId,
            actionType,
            cancellationToken));
    }
}
