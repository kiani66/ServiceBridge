using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Tickets;

public sealed class CreateTicketCommand
{
    private readonly ITicketService _ticketService;

    public CreateTicketCommand(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    public Task<ServiceResult<TicketDetailDto>> ExecuteAsync(
        Guid userId,
        CreateTicketRequest request)
    {
        return _ticketService.CreateTicketAsync(userId, request);
    }
}
