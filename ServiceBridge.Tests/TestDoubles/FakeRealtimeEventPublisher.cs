using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakeRealtimeEventPublisher : IRealtimeEventPublisher
{
    public List<RealtimeEventDto> Notifications { get; } = new();
    public List<OrderUpdatedRealtimeEvent> OrderUpdates { get; } = new();
    public List<OrderAssignedRealtimeEvent> OrderAssignments { get; } = new();
    public List<CustomerActionChangedRealtimeEvent> CustomerActionChanges { get; } = new();
    public List<TicketUpdatedRealtimeEvent> TicketUpdates { get; } = new();
    public bool ThrowOnOrderUpdate { get; set; }

    public Task PublishNotificationReceivedAsync(
        Guid userId,
        RealtimeEventDto notification,
        CancellationToken cancellationToken = default)
    {
        Notifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task PublishOrderUpdatedAsync(
        OrderUpdatedRealtimeEvent orderUpdated,
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnOrderUpdate)
        {
            throw new InvalidOperationException("Realtime notification failed.");
        }

        OrderUpdates.Add(orderUpdated);
        return Task.CompletedTask;
    }

    public Task PublishOrderAssignedAsync(
        OrderAssignedRealtimeEvent orderAssigned,
        CancellationToken cancellationToken = default)
    {
        OrderAssignments.Add(orderAssigned);
        return Task.CompletedTask;
    }

    public Task PublishCustomerActionChangedAsync(
        CustomerActionChangedRealtimeEvent customerActionChanged,
        CancellationToken cancellationToken = default)
    {
        CustomerActionChanges.Add(customerActionChanged);
        return Task.CompletedTask;
    }

    public Task PublishTicketUpdatedAsync(
        TicketUpdatedRealtimeEvent ticketUpdated,
        CancellationToken cancellationToken = default)
    {
        TicketUpdates.Add(ticketUpdated);
        return Task.CompletedTask;
    }
}
