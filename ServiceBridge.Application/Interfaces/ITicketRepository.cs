using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface ITicketRepository
{
    Task<bool> UserExistsAsync(Guid userId);
    Task<bool> OrderBelongsToUserAsync(Guid userId, Guid orderId);
    Task<bool> CategoryExistsAsync(Guid categoryId);
    Task<bool> TicketNumberExistsAsync(string ticketNumber);
    Task<PagedResultDto<TicketListItemDto>> GetMyTicketsAsync(
        Guid userId,
        int page,
        int pageSize,
        TicketStatus? status);
    Task<PagedResultDto<TicketListItemDto>> GetAdminTicketsAsync(
        int page,
        int pageSize,
        TicketStatus? status,
        TicketPriority? priority,
        string? search);
    Task<TicketDetailDto?> GetTicketDetailAsync(
        Guid ticketId,
        Guid? userId,
        bool includeInternalMessages);
    Task<Ticket?> GetTicketForUpdateAsync(Guid ticketId, Guid? userId);
    Task AddTicketAsync(Ticket ticket);
    Task<bool> SaveChangesAsync();
}
