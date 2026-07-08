using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class NotificationServiceTests
{
    [Fact]
    public async Task QueuePaymentCompletedAsync_StoresNotificationAndPublishesRealtimeAfterSave()
    {
        var repository = new FakeNotificationRepository();
        var realtimePublisher = new FakeRealtimeEventPublisher();
        var service = new NotificationService(repository, realtimePublisher);
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        await service.QueuePaymentCompletedAsync(userId, orderId, "SB-1001");
        repository.UnreadCounts[userId] = 3;
        await service.PublishQueuedRealtimeAsync();

        var notification = Assert.Single(repository.Added);
        Assert.Equal(userId, notification.UserId);
        Assert.Equal(orderId, notification.RelatedEntityId);
        Assert.Equal(NotificationType.PaymentCompleted, notification.Type);

        var realtimeEvent = Assert.Single(realtimePublisher.Notifications);
        Assert.Equal(RealtimeEventTypes.NotificationReceived, realtimeEvent.EventType);
        Assert.Equal(orderId, realtimeEvent.EntityId);
        Assert.Equal("Order", realtimeEvent.EntityName);
        Assert.Equal($"/ui/orders/{orderId}", realtimeEvent.LinkUrl);
        Assert.Equal(3, realtimeEvent.UnreadCount);
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public List<Notification> Added { get; } = new();
        public Dictionary<Guid, int> UnreadCounts { get; } = new();

        public Task<PagedResultDto<NotificationDto>> GetForUserAsync(
            Guid userId,
            int page,
            int pageSize,
            bool unreadOnly)
        {
            return Task.FromResult(new PagedResultDto<NotificationDto>());
        }

        public Task<int> GetUnreadCountAsync(Guid userId)
        {
            return Task.FromResult(
                UnreadCounts.TryGetValue(userId, out var count) ? count : 0);
        }

        public Task<Notification?> GetForUpdateAsync(Guid userId, Guid notificationId)
        {
            return Task.FromResult<Notification?>(null);
        }

        public Task<IReadOnlyCollection<Notification>> GetUnreadForUpdateAsync(Guid userId)
        {
            return Task.FromResult<IReadOnlyCollection<Notification>>(Array.Empty<Notification>());
        }

        public Task<IReadOnlyCollection<Guid>> GetActiveAdminUserIdsAsync()
        {
            return Task.FromResult<IReadOnlyCollection<Guid>>(Array.Empty<Guid>());
        }

        public Task AddAsync(Notification notification)
        {
            Added.Add(notification);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<Notification> notifications)
        {
            Added.AddRange(notifications);
            return Task.CompletedTask;
        }

        public Task<bool> SaveChangesAsync()
        {
            return Task.FromResult(true);
        }
    }
}
