using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeNotificationService : INotificationService
{
    public List<string> QueuedTypes { get; } = new();
    public List<FakeQueuedOrderCompletedNotification> QueuedOrderCompletions { get; } = new();
    public IList<string>? Operations { get; set; }
    public bool ThrowOnPublishQueuedRealtime { get; set; }

    public Task<ServiceResult<PagedResultDto<NotificationDto>>> GetForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly)
    {
        return Task.FromResult(ServiceResult<PagedResultDto<NotificationDto>>.Ok(
            new PagedResultDto<NotificationDto>()));
    }

    public Task<ServiceResult<UnreadNotificationCountDto>> GetUnreadCountAsync(
        Guid userId)
    {
        return Task.FromResult(ServiceResult<UnreadNotificationCountDto>.Ok(
            new UnreadNotificationCountDto()));
    }

    public Task<ServiceResult> MarkAsReadAsync(Guid userId, Guid notificationId)
    {
        return Task.FromResult(ServiceResult.Ok());
    }

    public Task<ServiceResult> MarkAllAsReadAsync(Guid userId)
    {
        return Task.FromResult(ServiceResult.Ok());
    }

    public Task PublishQueuedRealtimeAsync(CancellationToken cancellationToken = default)
    {
        QueuedTypes.Add(nameof(PublishQueuedRealtimeAsync));
        Operations?.Add("notification.publish");
        if (ThrowOnPublishQueuedRealtime)
        {
            throw new InvalidOperationException("Notification realtime failed.");
        }

        return Task.CompletedTask;
    }

    public Task QueueOrderCreatedAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        QueuedTypes.Add(nameof(QueueOrderCreatedAsync));
        return Task.CompletedTask;
    }

    public Task QueuePaymentCompletedAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        QueuedTypes.Add(nameof(QueuePaymentCompletedAsync));
        Operations?.Add("notification.payment.queued");
        return Task.CompletedTask;
    }

    public Task QueueOrderReadyForAssignmentAsync(
        Guid orderId,
        string orderNumber)
    {
        QueuedTypes.Add(nameof(QueueOrderReadyForAssignmentAsync));
        Operations?.Add("notification.assignment.queued");
        return Task.CompletedTask;
    }

    public Task QueueOrderAssignedAsync(
        Guid userId,
        Guid agentId,
        Guid orderId,
        string orderNumber)
    {
        QueuedTypes.Add(nameof(QueueOrderAssignedAsync));
        return Task.CompletedTask;
    }

    public Task QueueAgentStartedProcessingAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        QueuedTypes.Add(nameof(QueueAgentStartedProcessingAsync));
        return Task.CompletedTask;
    }

    public Task QueueOrderCompletedAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        QueuedTypes.Add(nameof(QueueOrderCompletedAsync));
        Operations?.Add("notification.completed.queued");
        QueuedOrderCompletions.Add(new(userId, orderId, orderNumber));
        return Task.CompletedTask;
    }

    public Task QueueOrderFailedAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        QueuedTypes.Add(nameof(QueueOrderFailedAsync));
        return Task.CompletedTask;
    }

    public Task QueueCustomerActionRequestedAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        QueuedTypes.Add(nameof(QueueCustomerActionRequestedAsync));
        return Task.CompletedTask;
    }

    public Task QueueCustomerActionRespondedAsync(
        Guid agentId,
        Guid orderId,
        string orderNumber)
    {
        QueuedTypes.Add(nameof(QueueCustomerActionRespondedAsync));
        return Task.CompletedTask;
    }

    public Task QueueTicketCreatedForAdminsAsync(
        Guid ticketId,
        string ticketNumber)
    {
        QueuedTypes.Add(nameof(QueueTicketCreatedForAdminsAsync));
        return Task.CompletedTask;
    }

    public Task QueueTicketUserReplyForAdminsAsync(
        Guid ticketId,
        string ticketNumber)
    {
        QueuedTypes.Add(nameof(QueueTicketUserReplyForAdminsAsync));
        return Task.CompletedTask;
    }

    public Task QueueTicketPublicReplyForUserAsync(
        Guid userId,
        Guid ticketId,
        string ticketNumber)
    {
        QueuedTypes.Add(nameof(QueueTicketPublicReplyForUserAsync));
        return Task.CompletedTask;
    }

    public Task QueueTicketClosedForUserAsync(
        Guid userId,
        Guid ticketId,
        string ticketNumber)
    {
        QueuedTypes.Add(nameof(QueueTicketClosedForUserAsync));
        return Task.CompletedTask;
    }

    public Task QueueRefundRequestedForAdminsAsync(
        Guid orderId,
        string orderNumber)
    {
        QueuedTypes.Add(nameof(QueueRefundRequestedForAdminsAsync));
        return Task.CompletedTask;
    }

    public Task QueueRefundApprovedForUserAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        QueuedTypes.Add(nameof(QueueRefundApprovedForUserAsync));
        return Task.CompletedTask;
    }

    public Task QueueRefundRejectedForUserAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        QueuedTypes.Add(nameof(QueueRefundRejectedForUserAsync));
        return Task.CompletedTask;
    }
}

internal sealed record FakeQueuedOrderCompletedNotification(
    Guid UserId,
    Guid OrderId,
    string OrderNumber);
