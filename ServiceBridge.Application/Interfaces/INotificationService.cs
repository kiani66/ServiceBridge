using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface INotificationService
{
    const int DefaultPageSize = 20;
    const int MaximumPageSize = 50;

    Task<ServiceResult<PagedResultDto<NotificationDto>>> GetForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly);

    Task<ServiceResult<UnreadNotificationCountDto>> GetUnreadCountAsync(Guid userId);
    Task<ServiceResult> MarkAsReadAsync(Guid userId, Guid notificationId);
    Task<ServiceResult> MarkAllAsReadAsync(Guid userId);
    Task PublishQueuedRealtimeAsync(CancellationToken cancellationToken = default);
    Task QueueOrderCreatedAsync(Guid userId, Guid orderId, string orderNumber);
    Task QueuePaymentCompletedAsync(Guid userId, Guid orderId, string orderNumber);
    Task QueueOrderReadyForAssignmentAsync(Guid orderId, string orderNumber);
    Task QueueOrderAssignedAsync(
        Guid userId,
        Guid agentId,
        Guid orderId,
        string orderNumber);
    Task QueueAgentStartedProcessingAsync(
        Guid userId,
        Guid orderId,
        string orderNumber);
    Task QueueOrderCompletedAsync(Guid userId, Guid orderId, string orderNumber);
    Task QueueOrderFailedAsync(Guid userId, Guid orderId, string orderNumber);
    Task QueueCustomerActionRequestedAsync(
        Guid userId,
        Guid orderId,
        string orderNumber);
    Task QueueCustomerActionRespondedAsync(
        Guid agentId,
        Guid orderId,
        string orderNumber);
    Task QueueTicketCreatedForAdminsAsync(Guid ticketId, string ticketNumber);
    Task QueueTicketPublicReplyForUserAsync(
        Guid userId,
        Guid ticketId,
        string ticketNumber);
    Task QueueTicketClosedForUserAsync(
        Guid userId,
        Guid ticketId,
        string ticketNumber);
    Task QueueRefundRequestedForAdminsAsync(Guid orderId, string orderNumber);
    Task QueueRefundApprovedForUserAsync(
        Guid userId,
        Guid orderId,
        string orderNumber);
    Task QueueRefundRejectedForUserAsync(
        Guid userId,
        Guid orderId,
        string orderNumber);
}
