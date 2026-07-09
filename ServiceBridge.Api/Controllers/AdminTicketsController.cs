using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Tickets;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.Admin)]
[Route("api/admin/tickets")]
public class AdminTicketsController : BaseApiController
{
    private readonly GetAdminTicketsQuery _getAdminTicketsQuery;
    private readonly GetAdminTicketDetailQuery _getAdminTicketDetailQuery;
    private readonly AdminReplyTicketCommand _adminReplyTicketCommand;
    private readonly CloseTicketCommand _closeTicketCommand;

    public AdminTicketsController(
        GetAdminTicketsQuery getAdminTicketsQuery,
        GetAdminTicketDetailQuery getAdminTicketDetailQuery,
        AdminReplyTicketCommand adminReplyTicketCommand,
        CloseTicketCommand closeTicketCommand)
    {
        _getAdminTicketsQuery = getAdminTicketsQuery;
        _getAdminTicketDetailQuery = getAdminTicketDetailQuery;
        _adminReplyTicketCommand = adminReplyTicketCommand;
        _closeTicketCommand = closeTicketCommand;
    }

    [HttpGet]
    public async Task<IActionResult> GetTickets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ITicketService.DefaultPageSize,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] Guid? orderId = null,
        [FromQuery] string? search = null)
    {
        return ToActionResult(await _getAdminTicketsQuery.ExecuteAsync(
            page,
            pageSize,
            status,
            userId,
            orderId,
            search));
    }

    [HttpGet("{ticketId:guid}")]
    public async Task<IActionResult> GetTicket(Guid ticketId)
    {
        return ToActionResult(await _getAdminTicketDetailQuery.ExecuteAsync(ticketId));
    }

    [HttpPost("{ticketId:guid}/reply")]
    public async Task<IActionResult> Reply(
        Guid ticketId,
        [FromBody] CreateTicketMessageRequest request)
    {
        if (!TryGetUserId(out var adminId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _adminReplyTicketCommand.ExecuteAsync(
            adminId,
            ticketId,
            request));
    }

    [HttpPost("{ticketId:guid}/close")]
    public async Task<IActionResult> Close(Guid ticketId)
    {
        if (!TryGetUserId(out var adminId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _closeTicketCommand.ExecuteAsync(
            adminId,
            ticketId));
    }
}
