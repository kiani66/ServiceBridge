using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.Interfaces;

public interface IOrderStatusPolicy
{
    bool CanTransition(OrderStatus currentStatus, OrderStatus newStatus);
    bool IsTerminal(OrderStatus status);
    IReadOnlyCollection<OrderStatus> GetAllowedTransitions(OrderStatus currentStatus);
}
