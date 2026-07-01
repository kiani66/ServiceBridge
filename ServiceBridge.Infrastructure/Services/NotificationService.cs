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

    private Task QueueAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        Guid orderId)
    {
        return _repository.AddAsync(Create(userId, type, title, message, orderId));
    }

    private static Notification Create(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        Guid orderId)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            RelatedEntityType = OrderEntityType,
            RelatedEntityId = orderId,
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
