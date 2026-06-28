using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Orders;

public static class OrderStatusTransitions
{
    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> Transitions =
        new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Created] =
            [
                OrderStatus.WaitingForPayment,
                OrderStatus.Cancelled
            ],
            [OrderStatus.WaitingForPayment] =
            [
                OrderStatus.Paid,
                OrderStatus.Cancelled
            ],
            [OrderStatus.Paid] =
            [
                OrderStatus.Assigned,
                OrderStatus.Cancelled
            ],
            [OrderStatus.Assigned] =
            [
                OrderStatus.Processing,
                OrderStatus.Cancelled
            ],
            [OrderStatus.Processing] =
            [
                OrderStatus.Completed,
                OrderStatus.Failed
            ],
            [OrderStatus.Completed] = [],
            [OrderStatus.Cancelled] = [],
            [OrderStatus.Failed] = []
        };

    public static bool CanTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        return Transitions.TryGetValue(currentStatus, out var allowedStatuses) &&
               allowedStatuses.Contains(newStatus);
    }

    public static bool IsTerminal(OrderStatus status)
    {
        return Transitions.TryGetValue(status, out var allowedStatuses) &&
               allowedStatuses.Length == 0;
    }

    public static IReadOnlyCollection<OrderStatus> GetAllowedTransitions(OrderStatus status)
    {
        return Transitions.TryGetValue(status, out var allowedStatuses)
            ? Array.AsReadOnly(allowedStatuses)
            : Array.Empty<OrderStatus>();
    }
}
