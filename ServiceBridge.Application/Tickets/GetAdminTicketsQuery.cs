using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Tickets;

public sealed class GetAdminTicketsQuery
{
    private readonly ITicketService _ticketService;

    public GetAdminTicketsQuery(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    public Task<ServiceResult<PagedResultDto<TicketListItemDto>>> ExecuteAsync(
        int page,
        int pageSize,
        TicketStatus? status,
        Guid? userId,
        Guid? orderId,
        string? search)
    {
        return _ticketService.GetAdminTicketsAsync(
            page,
            pageSize,
            status,
            userId,
            orderId,
            search);
    }
}
