using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly ServiceBridgeDbContext _context;

    public TicketRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public Task<bool> UserExistsAsync(Guid userId)
    {
        return _context.Users.AnyAsync(x => x.Id == userId && x.IsActive);
    }

    public Task<bool> OrderBelongsToUserAsync(Guid userId, Guid orderId)
    {
        return _context.Orders.AnyAsync(x =>
            x.Id == orderId &&
            x.UserId == userId);
    }

    public Task<bool> TicketNumberExistsAsync(string ticketNumber)
    {
        return _context.Tickets.AnyAsync(x => x.TicketNumber == ticketNumber);
    }

    public async Task<PagedResultDto<TicketListItemDto>> GetMyTicketsAsync(
        Guid userId,
        int page,
        int pageSize,
        TicketStatus? status)
    {
        var query = _context.Tickets
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await ProjectListAsync(query, page, pageSize);
    }

    public async Task<PagedResultDto<TicketListItemDto>> GetAdminTicketsAsync(
        int page,
        int pageSize,
        TicketStatus? status,
        Guid? userId,
        Guid? orderId,
        string? search)
    {
        var query = _context.Tickets.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        if (orderId.HasValue)
        {
            query = query.Where(x => x.OrderId == orderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.TicketNumber.Contains(term) ||
                x.Subject.Contains(term) ||
                x.User.FullName.Contains(term) ||
                x.User.Email.Contains(term) ||
                (x.Order != null && x.Order.OrderNumber.Contains(term)));
        }

        return await ProjectListAsync(query, page, pageSize);
    }

    public async Task<TicketDetailDto?> GetTicketDetailAsync(
        Guid ticketId,
        Guid? userId,
        bool includeInternalMessages)
    {
        var ticket = await _context.Tickets
            .AsNoTracking()
            .Where(x =>
                x.Id == ticketId &&
                (!userId.HasValue || x.UserId == userId.Value))
            .Select(x => new TicketDetailDto
            {
                TicketId = x.Id,
                TicketNumber = x.TicketNumber,
                UserId = x.UserId,
                Subject = x.Subject,
                Status = x.Status.ToString(),
                Priority = x.Priority.ToString(),
                OrderId = x.OrderId,
                OrderNumber = x.Order != null ? x.Order.OrderNumber : null,
                CustomerName = x.User.FullName,
                CustomerEmail = x.User.Email,
                CreatedAt = x.CreatedAt,
                LastActivityAt = x.LastActivityAt,
                FirstResponseAt = x.FirstResponseAt,
                ClosedAt = x.ClosedAt
            })
            .FirstOrDefaultAsync();

        if (ticket == null)
        {
            return null;
        }

        var messagesQuery = _context.TicketMessages
            .AsNoTracking()
            .Where(x => x.TicketId == ticketId);

        if (!includeInternalMessages)
        {
            messagesQuery = messagesQuery.Where(x => !x.IsInternal);
        }

        ticket.Messages = await messagesQuery
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(x => new TicketMessageDto
            {
                Id = x.Id,
                AuthorId = x.AuthorId,
                AuthorName = x.Author.FullName,
                AuthorRole = x.Author.Role.ToString(),
                Body = x.Body,
                IsInternal = x.IsInternal,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return ticket;
    }

    public Task<Ticket?> GetTicketForUpdateAsync(Guid ticketId, Guid? userId)
    {
        return _context.Tickets
            .Include(x => x.Messages)
            .FirstOrDefaultAsync(x =>
                x.Id == ticketId &&
                (!userId.HasValue || x.UserId == userId.Value));
    }

    public async Task AddTicketAsync(Ticket ticket)
    {
        await _context.Tickets.AddAsync(ticket);
    }

    public async Task<bool> SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return false;
        }
    }

    private async Task<PagedResultDto<TicketListItemDto>> ProjectListAsync(
        IQueryable<Ticket> query,
        int page,
        int pageSize)
    {
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.LastActivityAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new TicketListItemDto
            {
                TicketId = x.Id,
                TicketNumber = x.TicketNumber,
                Subject = x.Subject,
                Status = x.Status.ToString(),
                Priority = x.Priority.ToString(),
                OrderId = x.OrderId,
                OrderNumber = x.Order != null ? x.Order.OrderNumber : null,
                CustomerName = x.User.FullName,
                CustomerEmail = x.User.Email,
                CreatedAt = x.CreatedAt,
                LastActivityAt = x.LastActivityAt
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<TicketListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
