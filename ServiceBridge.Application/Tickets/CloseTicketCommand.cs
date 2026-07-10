using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Tickets;

public sealed class CloseTicketCommand
{
    private readonly ITicketService _ticketService;

    public CloseTicketCommand(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    public Task<ServiceResult> ExecuteAsync(Guid adminId, Guid ticketId)
    {
        return _ticketService.CloseTicketAsync(adminId, ticketId);
    }
}
