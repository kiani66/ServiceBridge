using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Tickets;

public sealed class GetAdminTicketDetailQuery
{
    private readonly ITicketService _ticketService;

    public GetAdminTicketDetailQuery(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    public Task<ServiceResult<TicketDetailDto>> ExecuteAsync(Guid ticketId)
    {
        return _ticketService.GetAdminTicketAsync(ticketId);
    }
}
