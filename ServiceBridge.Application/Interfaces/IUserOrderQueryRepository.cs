using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface IUserOrderQueryRepository
{
    Task<PagedResultDto<UserOrderListItemDto>> GetMyOrdersAsync(
        Guid userId,
        int page,
        int pageSize,
        OrderStatus? status);
    Task<UserOrderDetailDto?> GetMyOrderDetailAsync(Guid userId, Guid orderId);
}
