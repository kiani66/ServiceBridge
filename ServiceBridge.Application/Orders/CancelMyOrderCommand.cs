using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Application.Orders;

public class CancelMyOrderCommand
{
    private readonly IUserOrderCommandRepository _repository;

    public CancelMyOrderCommand(IUserOrderCommandRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult> ExecuteAsync(Guid userId, Guid orderId)
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
            order.TransitionTo(
                OrderStatus.Cancelled,
                userId,
                DateTime.UtcNow,
                AppMessages.OrderCancelledByUser);
        }
        catch (InvalidOperationException)
        {
            return ServiceResult.Fail(
                AppMessages.OrderCannotBeCancelled,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        if (!await _repository.SaveChangesAsync())
        {
            return ServiceResult.Fail(
                AppMessages.OrderChangedDuringCancellation,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        return ServiceResult.Ok(AppMessages.OrderCancelled);
    }
}
