using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.Orders;

namespace ServiceBridge.Application.Services;

public class OrderStatusPolicy : IOrderStatusPolicy
{
    public bool CanTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        return OrderStatusTransitions.CanTransition(currentStatus, newStatus);
    }

    public bool IsTerminal(OrderStatus status)
    {
        return OrderStatusTransitions.IsTerminal(status);
    }

    public IReadOnlyCollection<OrderStatus> GetAllowedTransitions(OrderStatus currentStatus)
    {
        return OrderStatusTransitions.GetAllowedTransitions(currentStatus);
    }
}
