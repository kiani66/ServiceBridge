using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface ITicketService
{
    const int DefaultPageSize = 20;
    const int MaximumPageSize = 100;

    Task<ServiceResult<PagedResultDto<TicketListItemDto>>> GetMyTicketsAsync(
        Guid userId,
        int page,
        int pageSize,
        TicketStatus? status);

    Task<ServiceResult<TicketDetailDto>> GetMyTicketAsync(
        Guid userId,
        Guid ticketId);

    Task<ServiceResult<TicketDetailDto>> CreateTicketAsync(
        Guid userId,
        CreateTicketRequest request);

    Task<ServiceResult> AddUserMessageAsync(
        Guid userId,
        Guid ticketId,
        CreateTicketMessageRequest request);

    Task<ServiceResult<PagedResultDto<TicketListItemDto>>> GetAdminTicketsAsync(
        int page,
        int pageSize,
        TicketStatus? status,
        Guid? userId,
        Guid? orderId,
        string? search);

    Task<ServiceResult<TicketDetailDto>> GetAdminTicketAsync(Guid ticketId);

    Task<ServiceResult> AddAdminMessageAsync(
        Guid adminId,
        Guid ticketId,
        CreateTicketMessageRequest request);

    Task<ServiceResult> CloseTicketAsync(Guid adminId, Guid ticketId);
}
