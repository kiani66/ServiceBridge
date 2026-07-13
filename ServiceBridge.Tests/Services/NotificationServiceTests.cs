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

    [Fact]
    public async Task OrderNotifications_StoreRecipientSpecificLinkUrls()
    {
        var repository = new FakeNotificationRepository();
        var service = new NotificationService(repository, new FakeRealtimeEventPublisher());
        var userId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        repository.ActiveAdminUserIds.Add(adminId);

        await service.QueuePaymentCompletedAsync(userId, orderId, "SB-1");
        await service.QueueOrderReadyForAssignmentAsync(orderId, "SB-1");
        await service.QueueOrderAssignedAsync(userId, agentId, orderId, "SB-1");
        await service.QueueAgentStartedProcessingAsync(userId, orderId, "SB-1");
        await service.QueueOrderCompletedAsync(userId, orderId, "SB-1");
        await service.QueueOrderFailedAsync(userId, orderId, "SB-1");
        await service.QueueCustomerActionRequestedAsync(userId, orderId, "SB-1");
        await service.QueueCustomerActionRespondedAsync(agentId, orderId, "SB-1");

        Assert.All(
            repository.Added.Where(notification => notification.UserId == userId),
            notification => Assert.Equal($"/ui/orders/{orderId}", notification.LinkUrl));
        Assert.All(
            repository.Added.Where(notification => notification.UserId == agentId),
            notification => Assert.Equal($"/ui/agent/orders/{orderId}", notification.LinkUrl));
        Assert.Equal(
            $"/ui/admin/orders/{orderId}",
            Assert.Single(repository.Added.Where(notification =>
                notification.UserId == adminId &&
                notification.Title == "Order ready for assignment")).LinkUrl);
    }

    [Fact]
    public async Task TicketAndRefundNotifications_StoreRecipientSpecificLinkUrls()
    {
        var repository = new FakeNotificationRepository();
        var service = new NotificationService(repository, new FakeRealtimeEventPublisher());
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        repository.ActiveAdminUserIds.Add(adminId);

        await service.QueueTicketUserReplyForAdminsAsync(ticketId, "T-1");
        await service.QueueTicketPublicReplyForUserAsync(userId, ticketId, "T-1");
        await service.QueueRefundRequestedForAdminsAsync(orderId, "SB-1");
        await service.QueueRefundApprovedForUserAsync(userId, orderId, "SB-1");
        await service.QueueRefundRejectedForUserAsync(userId, orderId, "SB-1");

        Assert.Equal(
            $"/ui/admin/tickets/{ticketId}",
            Assert.Single(repository.Added.Where(notification =>
                notification.UserId == adminId &&
                notification.Type == NotificationType.TicketReplied)).LinkUrl);
        Assert.Equal(
            $"/ui/tickets/{ticketId}",
            Assert.Single(repository.Added.Where(notification =>
                notification.UserId == userId &&
                notification.Type == NotificationType.TicketReplied)).LinkUrl);
        Assert.Equal(
            $"/ui/admin/orders/{orderId}",
            Assert.Single(repository.Added.Where(notification =>
                notification.UserId == adminId &&
                notification.Type == NotificationType.RefundRequested)).LinkUrl);
        Assert.All(
            repository.Added.Where(notification =>
                notification.UserId == userId &&
                notification.Type is NotificationType.RefundApproved or NotificationType.RefundRejected),
            notification => Assert.Equal($"/ui/orders/{orderId}", notification.LinkUrl));
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public List<Notification> Added { get; } = new();
        public Dictionary<Guid, int> UnreadCounts { get; } = new();
        public List<Guid> ActiveAdminUserIds { get; } = new();

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
            return Task.FromResult<IReadOnlyCollection<Guid>>(ActiveAdminUserIds);
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
