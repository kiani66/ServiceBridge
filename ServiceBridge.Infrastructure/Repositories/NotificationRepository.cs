using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ServiceBridgeDbContext _context;

    public NotificationRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<NotificationDto>> GetForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(x => !x.IsRead);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.IsRead)
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                Title = x.Title,
                Body = x.Message,
                CreatedAtUtc = x.CreatedAt,
                IsRead = x.IsRead,
                LinkUrl = x.LinkUrl
            })
            .ToListAsync();

        return new PagedResultDto<NotificationDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public Task<int> GetUnreadCountAsync(Guid userId)
    {
        return _context.Notifications.CountAsync(x =>
            x.UserId == userId &&
            !x.IsRead);
    }

    public Task<Notification?> GetForUpdateAsync(
        Guid userId,
        Guid notificationId)
    {
        return _context.Notifications.FirstOrDefaultAsync(x =>
            x.Id == notificationId &&
            x.UserId == userId);
    }

    public async Task<IReadOnlyCollection<Notification>> GetUnreadForUpdateAsync(
        Guid userId)
    {
        return await _context.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync();
    }

    public async Task<IReadOnlyCollection<Guid>> GetActiveAdminUserIdsAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Where(x => x.Role == UserRole.Admin && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync();
    }

    public async Task AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
    }

    public async Task AddRangeAsync(IEnumerable<Notification> notifications)
    {
        await _context.Notifications.AddRangeAsync(notifications);
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
}
