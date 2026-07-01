using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;

namespace ServiceBridge.Application.Interfaces;

public interface INotificationRepository
{
    Task<PagedResultDto<NotificationDto>> GetForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly);

    Task<int> GetUnreadCountAsync(Guid userId);
    Task<Notification?> GetForUpdateAsync(Guid userId, Guid notificationId);
    Task<IReadOnlyCollection<Notification>> GetUnreadForUpdateAsync(Guid userId);
    Task<IReadOnlyCollection<Guid>> GetActiveAdminUserIdsAsync();
    Task AddAsync(Notification notification);
    Task AddRangeAsync(IEnumerable<Notification> notifications);
    Task<bool> SaveChangesAsync();
}
