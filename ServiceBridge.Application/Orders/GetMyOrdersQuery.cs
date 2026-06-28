using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Orders;

public class GetMyOrdersQuery
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    private readonly IUserOrderQueryRepository _repository;
    private readonly IOrderStatusPolicy _statusPolicy;

    public GetMyOrdersQuery(
        IUserOrderQueryRepository repository,
        IOrderStatusPolicy statusPolicy)
    {
        _repository = repository;
        _statusPolicy = statusPolicy;
    }

    public async Task<ServiceResult<PagedResultDto<UserOrderListItemDto>>> ExecuteAsync(
        Guid userId,
        int page,
        int pageSize,
        OrderStatus? status)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize <= 0
            ? DefaultPageSize
            : Math.Min(pageSize, MaximumPageSize);

        var result = await _repository.GetMyOrdersAsync(
            userId,
            page,
            pageSize,
            status);

        foreach (var order in result.Items)
        {
            order.CanCancel = Enum.TryParse<OrderStatus>(
                                  order.Status,
                                  out var orderStatus) &&
                              _statusPolicy.CanTransition(
                                  orderStatus,
                                  OrderStatus.Cancelled);
        }

        return ServiceResult<PagedResultDto<UserOrderListItemDto>>.Ok(result);
    }
}
