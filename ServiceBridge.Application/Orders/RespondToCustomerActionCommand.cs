using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Application.Orders;

public class RespondToCustomerActionCommand
{
    private readonly IUserOrderCommandRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly IRealtimeEventPublisher _realtimeEventPublisher;

    public RespondToCustomerActionCommand(
        IUserOrderCommandRepository repository,
        INotificationService notificationService,
        IRealtimeEventPublisher realtimeEventPublisher)
    {
        _repository = repository;
        _notificationService = notificationService;
        _realtimeEventPublisher = realtimeEventPublisher;
    }

    public async Task<ServiceResult> ExecuteAsync(
        Guid userId,
        Guid orderId,
        RespondToCustomerActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Response))
        {
            return ServiceResult.ValidationFailed(
                new Dictionary<string, string[]>
                {
                    [nameof(request.Response)] =
                    [
                        "Response is required."
                    ]
                });
        }

        var order = await _repository.GetOwnedOrderForUpdateAsync(userId, orderId);
        if (order == null)
        {
            return ServiceResult.Fail(
                AppMessages.OrderNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        try
        {
            order.RespondToCustomerAction(
                userId,
                request.RequestId,
                request.Response);
        }
        catch (InvalidOperationException)
        {
            return ServiceResult.Fail(
                AppMessages.CustomerActionResponseRejected,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var requestAgentId = order.CustomerActionRequests
            .Single(x => x.Id == request.RequestId)
            .RequestedByAgentId;
        var notificationAgentId = order.AssignedAgentId ?? requestAgentId;
        await _notificationService.QueueCustomerActionRespondedAsync(
            notificationAgentId,
            order.Id,
            order.OrderNumber);

        if (!await _repository.SaveChangesAsync())
        {
            return ServiceResult.Fail(
                AppMessages.OrderChangedDuringCustomerAction,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        await _notificationService.PublishQueuedRealtimeAsync();
        await _realtimeEventPublisher.PublishCustomerActionChangedAsync(
            new CustomerActionChangedRealtimeEvent
            {
                OrderId = order.Id,
                RequestId = request.RequestId,
                LinkUrl = $"/ui/agent/orders/{order.Id}",
                UpdatedAtUtc = order.UpdatedAt ?? DateTime.UtcNow,
                UserId = order.UserId,
                AgentId = notificationAgentId
            });

        return ServiceResult.Ok(AppMessages.CustomerActionResponded);
    }
}
