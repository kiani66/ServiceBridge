using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Tickets;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.User)]
[Route("api/tickets")]
public class TicketsController : BaseApiController
{
    private readonly GetMyTicketsQuery _getMyTicketsQuery;
    private readonly GetMyTicketDetailQuery _getMyTicketDetailQuery;
    private readonly CreateTicketCommand _createTicketCommand;
    private readonly ReplyToTicketCommand _replyToTicketCommand;

    public TicketsController(
        GetMyTicketsQuery getMyTicketsQuery,
        GetMyTicketDetailQuery getMyTicketDetailQuery,
        CreateTicketCommand createTicketCommand,
        ReplyToTicketCommand replyToTicketCommand)
    {
        _getMyTicketsQuery = getMyTicketsQuery;
        _getMyTicketDetailQuery = getMyTicketDetailQuery;
        _createTicketCommand = createTicketCommand;
        _replyToTicketCommand = replyToTicketCommand;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ITicketService.DefaultPageSize,
        [FromQuery] TicketStatus? status = null)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _getMyTicketsQuery.ExecuteAsync(
            userId,
            page,
            pageSize,
            status));
    }

    [HttpGet("{ticketId:guid}")]
    public async Task<IActionResult> GetMyTicket(Guid ticketId)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _getMyTicketDetailQuery.ExecuteAsync(
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

        return ToActionResult(await _createTicketCommand.ExecuteAsync(
            userId,
            request));
    }

    [HttpPost("{ticketId:guid}/reply")]
    public async Task<IActionResult> Reply(
        Guid ticketId,
        [FromBody] CreateTicketMessageRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _replyToTicketCommand.ExecuteAsync(
            userId,
            ticketId,
            request));
    }
}
