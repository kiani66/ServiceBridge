using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Application.Orders;

public class RespondToCustomerActionCommand
{
    private readonly IUserOrderCommandRepository _repository;

    public RespondToCustomerActionCommand(IUserOrderCommandRepository repository)
    {
        _repository = repository;
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
