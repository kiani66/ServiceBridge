using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.Admin)]
[Route("api/admin/orders")]
public class AdminOrdersController : BaseApiController
{
    private readonly IAdminOrderQueryService _queryService;
    private readonly IAdminOrderAssignmentService _assignmentService;

    public AdminOrdersController(
        IAdminOrderQueryService queryService,
        IAdminOrderAssignmentService assignmentService)
    {
        _queryService = queryService;
        _assignmentService = assignmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = IAdminOrderQueryService.DefaultPageSize,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] string? search = null)
    {
        return ToActionResult(await _queryService.GetOrdersAsync(
            page,
            pageSize,
            status,
            search));
    }

    [HttpGet("paid-unassigned")]
    public async Task<IActionResult> GetPaidUnassignedOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = IAdminOrderQueryService.DefaultPageSize)
    {
        return ToActionResult(
            await _queryService.GetPaidUnassignedOrdersAsync(page, pageSize));
    }

    [HttpGet("agents")]
    public async Task<IActionResult> GetActiveAgents()
    {
        return ToActionResult(await _queryService.GetActiveAgentsAsync());
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        return ToActionResult(await _queryService.GetDashboardSummaryAsync());
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = IAdminOrderQueryService.DefaultPageSize,
        [FromQuery] PaymentStatus? status = null,
        [FromQuery] string? search = null)
    {
        return ToActionResult(await _queryService.GetPaymentsAsync(
            page,
            pageSize,
            status,
            search));
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditEvents(
        [FromQuery] AdminAuditQueryDto query)
    {
        return ToActionResult(await _queryService.GetAuditEventsAsync(query));
    }

    [HttpGet("{orderId:guid}/timeline")]
    public async Task<IActionResult> GetOrderTimeline(Guid orderId)
    {
        return ToActionResult(await _queryService.GetOrderTimelineAsync(orderId));
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrderDetail(Guid orderId)
    {
        if (!TryGetUserId(out var adminId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _queryService.GetOrderDetailAsync(
            adminId,
            orderId));
    }

    [HttpPost("{orderId:guid}/assign")]
    public async Task<IActionResult> Assign(
        Guid orderId,
        [FromBody] AssignOrderRequest request)
    {
        if (!TryGetUserId(out var adminId))
        {
            return UnauthorizedServiceResult();
        }

        var result = await _assignmentService.AssignAsync(
            adminId,
            orderId,
            request);
        return ToActionResult(result);
    }
}
