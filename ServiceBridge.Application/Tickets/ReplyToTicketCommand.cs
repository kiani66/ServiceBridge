using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Tickets;

public sealed class ReplyToTicketCommand
{
    private readonly ITicketService _ticketService;

    public ReplyToTicketCommand(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    public Task<ServiceResult> ExecuteAsync(
        Guid userId,
        Guid ticketId,
        CreateTicketMessageRequest request)
    {
        request.IsInternal = false;
        return _ticketService.AddUserMessageAsync(userId, ticketId, request);
    }
}
