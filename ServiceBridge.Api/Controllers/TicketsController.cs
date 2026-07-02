using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.User)]
[Route("api/tickets")]
public class TicketsController : BaseApiController
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ITicketService.DefaultPageSize,
        [FromQuery] TicketStatus? status = null)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _ticketService.GetMyTicketsAsync(
            userId,
            page,
            pageSize,
            status));
    }

    [HttpGet("my/{ticketId:guid}")]
    public async Task<IActionResult> GetMyTicket(Guid ticketId)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _ticketService.GetMyTicketAsync(
            userId,
            ticketId));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _ticketService.CreateTicketAsync(
            userId,
            request));
    }

    [HttpPost("{ticketId:guid}/messages")]
    public async Task<IActionResult> AddMessage(
        Guid ticketId,
        [FromBody] CreateTicketMessageRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _ticketService.AddUserMessageAsync(
            userId,
            ticketId,
            request));
    }
}
