using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.Admin)]
[Route("api/admin/tickets")]
public class AdminTicketsController : BaseApiController
{
    private readonly ITicketService _ticketService;

    public AdminTicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTickets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ITicketService.DefaultPageSize,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] string? search = null)
    {
        return ToActionResult(await _ticketService.GetAdminTicketsAsync(
            page,
            pageSize,
            status,
            priority,
            search));
    }

    [HttpGet("{ticketId:guid}")]
    public async Task<IActionResult> GetTicket(Guid ticketId)
    {
        return ToActionResult(await _ticketService.GetAdminTicketAsync(ticketId));
    }

    [HttpPost("{ticketId:guid}/messages")]
    public async Task<IActionResult> AddMessage(
        Guid ticketId,
        [FromBody] CreateTicketMessageRequest request)
    {
        if (!TryGetUserId(out var adminId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _ticketService.AddAdminMessageAsync(
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

        return ToActionResult(await _ticketService.CloseTicketAsync(
            adminId,
            ticketId));
    }

    [HttpPost("{ticketId:guid}/priority")]
    public async Task<IActionResult> UpdatePriority(
        Guid ticketId,
        [FromBody] UpdateTicketPriorityRequest request)
    {
        if (!TryGetUserId(out var adminId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _ticketService.UpdatePriorityAsync(
            adminId,
            ticketId,
            request));
    }
}
