using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Tickets;

public sealed class GetMyTicketDetailQuery
{
    private readonly ITicketService _ticketService;

    public GetMyTicketDetailQuery(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    public Task<ServiceResult<TicketDetailDto>> ExecuteAsync(
        Guid userId,
        Guid ticketId)
    {
        return _ticketService.GetMyTicketAsync(userId, ticketId);
    }
}
