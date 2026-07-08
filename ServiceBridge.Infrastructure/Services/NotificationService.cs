using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private const string OrderEntityType = "Order";
    private const string TicketEntityType = "Ticket";

    private readonly INotificationRepository _repository;

    public NotificationService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<PagedResultDto<NotificationDto>>> GetForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly)
    {
        page = Math.Max(page, 1);
        pageSize = NormalizePageSize(pageSize);

        var notifications = await _repository.GetForUserAsync(
            userId,
            page,
            pageSize,
            unreadOnly);

        return ServiceResult<PagedResultDto<NotificationDto>>.Ok(notifications);
    }

    public async Task<ServiceResult<UnreadNotificationCountDto>> GetUnreadCountAsync(
        Guid userId)
    {
        var count = await _repository.GetUnreadCountAsync(userId);
        return ServiceResult<UnreadNotificationCountDto>.Ok(
            new UnreadNotificationCountDto { Count = count });
    }

    public async Task<ServiceResult> MarkAsReadAsync(
        Guid userId,
        Guid notificationId)
    {
        var notification = await _repository.GetForUpdateAsync(
            userId,
            notificationId);
        if (notification == null)
        {
            return ServiceResult.Fail(
                "Notification not found.",
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        notification.MarkRead(DateTime.UtcNow);
        return await _repository.SaveChangesAsync()
            ? ServiceResult.Ok("Notification marked as read.")
            : ServiceResult.Fail(
                "Notification changed while being updated.",
                AppConstants.ErrorCodes.Conflict,
                409);
    }

    public async Task<ServiceResult> MarkAllAsReadAsync(Guid userId)
    {
        var notifications = await _repository.GetUnreadForUpdateAsync(userId);
        var now = DateTime.UtcNow;

        foreach (var notification in notifications)
        {
            notification.MarkRead(now);
        }

        return await _repository.SaveChangesAsync()
            ? ServiceResult.Ok("Notifications marked as read.")
            : ServiceResult.Fail(
                "Notifications changed while being updated.",
                AppConstants.ErrorCodes.Conflict,
                409);
    }

    public Task QueueOrderCreatedAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        return QueueAsync(
            userId,
            NotificationType.OrderCreated,
            "Order created",
            $"Order {orderNumber} was created and is waiting for payment.",
            orderId);
    }

    public Task QueuePaymentCompletedAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        return QueueAsync(
            userId,
            NotificationType.PaymentCompleted,
            "Payment confirmed",
            $"Payment for order {orderNumber} was confirmed.",
            orderId);
    }

    public async Task QueueOrderReadyForAssignmentAsync(
        Guid orderId,
        string orderNumber)
    {
        var adminIds = await _repository.GetActiveAdminUserIdsAsync();
        var notifications = adminIds.Select(adminId => Create(
            adminId,
            NotificationType.PaymentCompleted,
            "Order ready for assignment",
            $"Order {orderNumber} is paid and ready for assignment.",
            orderId));

        await _repository.AddRangeAsync(notifications);
    }

    public async Task QueueOrderAssignedAsync(
        Guid userId,
        Guid agentId,
        Guid orderId,
        string orderNumber)
    {
        await _repository.AddRangeAsync([
            Create(
                userId,
                NotificationType.OrderAssignedToAgent,
                "Order assigned",
                $"Order {orderNumber} was assigned to an agent.",
                orderId),
            Create(
                agentId,
                NotificationType.OrderAssignedToAgent,
                "New assigned order",
                $"Order {orderNumber} was assigned to you.",
                orderId)
        ]);
    }

    public Task QueueAgentStartedProcessingAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        return QueueAsync(
            userId,
            NotificationType.AgentStartedProcessing,
            "Processing started",
            $"An agent started processing order {orderNumber}.",
            orderId);
    }

    public Task QueueOrderCompletedAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        return QueueAsync(
            userId,
            NotificationType.OrderCompleted,
            "Order completed",
            $"Order {orderNumber} has been completed.",
            orderId);
    }

    public Task QueueOrderFailedAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        return QueueAsync(
            userId,
            NotificationType.OrderFailed,
            "Order failed",
            $"Order {orderNumber} could not be completed.",
            orderId);
    }

    public Task QueueCustomerActionRequestedAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        return QueueAsync(
            userId,
            NotificationType.CustomerActionRequested,
            "Customer action requested",
            $"Order {orderNumber} needs your response before processing can continue.",
            orderId);
    }

    public Task QueueCustomerActionRespondedAsync(
        Guid agentId,
        Guid orderId,
        string orderNumber)
    {
        return QueueAsync(
            agentId,
            NotificationType.CustomerActionResponded,
            "Customer response received",
            $"Customer information was provided for order {orderNumber}.",
            orderId);
    }

    public async Task QueueTicketCreatedForAdminsAsync(
        Guid ticketId,
        string ticketNumber)
    {
        var adminIds = await _repository.GetActiveAdminUserIdsAsync();
        var notifications = adminIds.Select(adminId => Create(
            adminId,
            NotificationType.TicketCreated,
            "New support ticket",
            $"Ticket {ticketNumber} was created and is waiting for support.",
            TicketEntityType,
            ticketId));

        await _repository.AddRangeAsync(notifications);
    }

    public Task QueueTicketPublicReplyForUserAsync(
        Guid userId,
        Guid ticketId,
        string ticketNumber)
    {
        return QueueAsync(
            userId,
            NotificationType.TicketReplied,
            "Support replied",
            $"Support replied to ticket {ticketNumber}.",
            TicketEntityType,
            ticketId);
    }

    public Task QueueTicketClosedForUserAsync(
        Guid userId,
        Guid ticketId,
        string ticketNumber)
    {
        return QueueAsync(
            userId,
            NotificationType.TicketClosed,
            "Ticket closed",
            $"Ticket {ticketNumber} was closed by support.",
            TicketEntityType,
            ticketId);
    }

    public async Task QueueRefundRequestedForAdminsAsync(
        Guid orderId,
        string orderNumber)
    {
        var adminIds = await _repository.GetActiveAdminUserIdsAsync();
        var notifications = adminIds.Select(adminId => Create(
            adminId,
            NotificationType.RefundRequested,
            "Refund requested",
            $"Order {orderNumber} has a new refund request.",
            orderId));

        await _repository.AddRangeAsync(notifications);
    }

    public Task QueueRefundApprovedForUserAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        return QueueAsync(
            userId,
            NotificationType.RefundApproved,
            "Refund approved",
            $"Refund request for order {orderNumber} was approved.",
            orderId);
    }

    public Task QueueRefundRejectedForUserAsync(
        Guid userId,
        Guid orderId,
        string orderNumber)
    {
        return QueueAsync(
            userId,
            NotificationType.RefundRejected,
            "Refund rejected",
            $"Refund request for order {orderNumber} was rejected.",
            orderId);
    }

    private Task QueueAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        Guid orderId)
    {
        return QueueAsync(userId, type, title, message, OrderEntityType, orderId);
    }

    private Task QueueAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string relatedEntityType,
        Guid relatedEntityId)
    {
        return _repository.AddAsync(Create(
            userId,
            type,
            title,
            message,
            relatedEntityType,
            relatedEntityId));
    }

    private static Notification Create(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        Guid orderId)
    {
        return Create(userId, type, title, message, OrderEntityType, orderId);
    }

    private static Notification Create(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        string relatedEntityType,
        Guid relatedEntityId)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static int NormalizePageSize(int pageSize)
    {
        return pageSize <= 0
            ? INotificationService.DefaultPageSize
            : Math.Min(pageSize, INotificationService.MaximumPageSize);
    }
}
