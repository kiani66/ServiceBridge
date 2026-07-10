using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Tickets;

public sealed class AdminReplyTicketCommand
{
    private readonly ITicketService _ticketService;

    public AdminReplyTicketCommand(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    public Task<ServiceResult> ExecuteAsync(
        Guid adminId,
        Guid ticketId,
        CreateTicketMessageRequest request)
    {
        return _ticketService.AddAdminMessageAsync(adminId, ticketId, request);
    }
}
