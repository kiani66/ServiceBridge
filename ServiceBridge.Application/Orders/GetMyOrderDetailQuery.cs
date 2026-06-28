using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Application.Orders;

public class GetMyOrderDetailQuery
{
    private readonly IUserOrderQueryRepository _repository;
    private readonly IOrderStatusPolicy _statusPolicy;

    public GetMyOrderDetailQuery(
        IUserOrderQueryRepository repository,
        IOrderStatusPolicy statusPolicy)
    {
        _repository = repository;
        _statusPolicy = statusPolicy;
    }

    public async Task<ServiceResult<UserOrderDetailDto>> ExecuteAsync(
        Guid userId,
        Guid orderId)
    {
        var order = await _repository.GetMyOrderDetailAsync(userId, orderId);
        if (order == null)
        {
            return ServiceResult<UserOrderDetailDto>.Fail(
                AppMessages.OrderNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        order.CanCancel = Enum.TryParse<OrderStatus>(order.Status, out var status) &&
                          _statusPolicy.CanTransition(status, OrderStatus.Cancelled);

        return ServiceResult<UserOrderDetailDto>.Ok(order);
    }
}
