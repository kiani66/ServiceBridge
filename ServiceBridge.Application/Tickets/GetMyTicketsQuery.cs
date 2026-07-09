using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Tickets;

public sealed class GetMyTicketsQuery
{
    private readonly ITicketService _ticketService;

    public GetMyTicketsQuery(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    public Task<ServiceResult<PagedResultDto<TicketListItemDto>>> ExecuteAsync(
        Guid userId,
        int page,
        int pageSize,
        TicketStatus? status)
    {
        return _ticketService.GetMyTicketsAsync(userId, page, pageSize, status);
    }
}
