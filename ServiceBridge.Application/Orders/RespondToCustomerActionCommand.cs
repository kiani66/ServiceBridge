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

    public RespondToCustomerActionCommand(
        IUserOrderCommandRepository repository,
        INotificationService notificationService)
    {
        _repository = repository;
        _notificationService = notificationService;
    }

    public async Task<ServiceResult> ExecuteAsync(
        Guid userId,
        Guid orderId,
        RespondToCustomerActionRequest request)
    {
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

        return ServiceResult.Ok(AppMessages.CustomerActionResponded);
    }
}
