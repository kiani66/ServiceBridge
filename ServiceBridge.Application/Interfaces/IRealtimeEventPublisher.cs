using ServiceBridge.Application.DTOs;

namespace ServiceBridge.Application.Interfaces;

public interface IRealtimeEventPublisher
{
    Task PublishNotificationReceivedAsync(
        Guid userId,
        RealtimeEventDto notification,
        CancellationToken cancellationToken = default);

    Task PublishOrderUpdatedAsync(
        OrderUpdatedRealtimeEvent orderUpdated,
        CancellationToken cancellationToken = default);

    Task PublishOrderAssignedAsync(
        OrderAssignedRealtimeEvent orderAssigned,
        CancellationToken cancellationToken = default);

    Task PublishCustomerActionChangedAsync(
        CustomerActionChangedRealtimeEvent customerActionChanged,
        CancellationToken cancellationToken = default);

    Task PublishTicketUpdatedAsync(
        TicketUpdatedRealtimeEvent ticketUpdated,
        CancellationToken cancellationToken = default);
}
