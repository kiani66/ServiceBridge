using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeUserOrderQueryRepository : IUserOrderQueryRepository
{
    public Dictionary<Guid, IReadOnlyCollection<UserOrderListItemDto>> OrdersByUser { get; } =
        new();
    public Dictionary<(Guid UserId, Guid OrderId), UserOrderDetailDto> Details { get; } =
        new();
    public Guid? LastListUserId { get; private set; }
    public Guid? LastDetailUserId { get; private set; }
    public Guid? LastDetailOrderId { get; private set; }
    public int? LastPage { get; private set; }
    public int? LastPageSize { get; private set; }
    public OrderStatus? LastStatus { get; private set; }

    public Task<PagedResultDto<UserOrderListItemDto>> GetMyOrdersAsync(
        Guid userId,
        int page,
        int pageSize,
        OrderStatus? status)
    {
        LastListUserId = userId;
        LastPage = page;
        LastPageSize = pageSize;
        LastStatus = status;

        var orders = OrdersByUser.TryGetValue(userId, out var userOrders)
            ? userOrders
            : Array.Empty<UserOrderListItemDto>();

        return Task.FromResult(new PagedResultDto<UserOrderListItemDto>
        {
            Items = orders,
            Page = page,
            PageSize = pageSize,
            TotalCount = orders.Count
        });
    }

    public Task<UserOrderDetailDto?> GetMyOrderDetailAsync(Guid userId, Guid orderId)
    {
        LastDetailUserId = userId;
        LastDetailOrderId = orderId;
        Details.TryGetValue((userId, orderId), out var order);
        return Task.FromResult(order);
    }
}
