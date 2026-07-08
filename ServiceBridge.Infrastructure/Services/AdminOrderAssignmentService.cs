using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class AdminOrderAssignmentService : IAdminOrderAssignmentService
{
    private readonly IAdminOrderCommandRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly IRealtimeEventPublisher _realtimeEventPublisher;

    public AdminOrderAssignmentService(
        IAdminOrderCommandRepository repository,
        INotificationService notificationService,
        IRealtimeEventPublisher realtimeEventPublisher)
    {
        _repository = repository;
        _notificationService = notificationService;
        _realtimeEventPublisher = realtimeEventPublisher;
    }

    public async Task<ServiceResult> AssignAsync(
        Guid adminId,
        Guid orderId,
        AssignOrderRequest request)
    {
        var order = await _repository.GetOrderForAssignmentAsync(orderId);
        if (order == null)
        {
            return ServiceResult.Fail(
                AppMessages.OrderNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (!await _repository.ActiveAgentExistsAsync(request.AgentId))
        {
            return ServiceResult.Fail(
                AppMessages.AgentNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        try
        {
            order.AssignToAgent(
                request.AgentId,
                adminId,
                request.Notes,
                AppMessages.OrderAssignedToAgent);
            await _notificationService.QueueOrderAssignedAsync(
                order.UserId,
                request.AgentId,
                order.Id,
                order.OrderNumber);
        }
        catch (InvalidOperationException)
        {
            return ServiceResult.Fail(
                AppMessages.OrderCannotBeAssigned,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        if (!await _repository.SaveChangesAsync())
        {
            return ServiceResult.Fail(
                AppMessages.OrderChangedDuringAssignment,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        await _notificationService.PublishQueuedRealtimeAsync();
        await _realtimeEventPublisher.PublishOrderAssignedAsync(
            new OrderAssignedRealtimeEvent
            {
                OrderId = order.Id,
                LinkUrl = $"/ui/agent/orders/{order.Id}",
                AssignedAtUtc = order.AssignedAt ?? DateTime.UtcNow,
                UserId = order.UserId,
                AgentId = request.AgentId
            });
        await _realtimeEventPublisher.PublishOrderUpdatedAsync(
            CreateOrderUpdatedEvent(order, notifyAdmins: true));

        return ServiceResult.Ok(AppMessages.OrderAssigned);
    }

    private static OrderUpdatedRealtimeEvent CreateOrderUpdatedEvent(
        Domain.Entities.Order order,
        bool notifyAdmins)
    {
        return new OrderUpdatedRealtimeEvent
        {
            OrderId = order.Id,
            Status = order.Status.ToString(),
            LinkUrl = $"/ui/orders/{order.Id}",
            UpdatedAtUtc = order.UpdatedAt ?? DateTime.UtcNow,
            UserId = order.UserId,
            AgentId = order.AssignedAgentId,
            NotifyAdmins = notifyAdmins
        };
    }

}
