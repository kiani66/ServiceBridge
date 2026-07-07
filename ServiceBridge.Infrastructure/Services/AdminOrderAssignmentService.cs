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
    private readonly IOrderRealtimeNotifier _orderRealtimeNotifier;

    public AdminOrderAssignmentService(
        IAdminOrderCommandRepository repository,
        INotificationService notificationService,
        IOrderRealtimeNotifier orderRealtimeNotifier)
    {
        _repository = repository;
        _notificationService = notificationService;
        _orderRealtimeNotifier = orderRealtimeNotifier;
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

        await _orderRealtimeNotifier.NotifyOrderUpdatedAsync(
            CreateRealtimeMessage(order));

        return ServiceResult.Ok(AppMessages.OrderAssigned);
    }

    private static OrderUpdatedRealtimeMessage CreateRealtimeMessage(
        Domain.Entities.Order order)
    {
        return new OrderUpdatedRealtimeMessage
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            NewStatus = order.Status.ToString(),
            UpdatedAtUtc = order.UpdatedAt ?? DateTime.UtcNow,
            UserId = order.UserId,
            AgentId = order.AssignedAgentId
        };
    }
}
