using Microsoft.AspNetCore.SignalR;
using ServiceBridge.Api.Hubs;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Api.Realtime;

public sealed class SignalRRealtimeEventPublisher : IRealtimeEventPublisher
{
    private const string NotificationReceivedMethod = "notificationReceived";
    private const string OrderUpdatedMethod = "orderUpdated";
    private const string OrderAssignedMethod = "orderAssigned";
    private const string CustomerActionChangedMethod = "customerActionChanged";
    private const string TicketUpdatedMethod = "ticketUpdated";

    private readonly IHubContext<RealtimeHub> _hubContext;
    private readonly ILogger<SignalRRealtimeEventPublisher> _logger;

    public SignalRRealtimeEventPublisher(
        IHubContext<RealtimeHub> hubContext,
        ILogger<SignalRRealtimeEventPublisher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task PublishNotificationReceivedAsync(
        Guid userId,
        RealtimeEventDto notification,
        CancellationToken cancellationToken = default)
    {
        return SendAsync(
            () => _hubContext.Clients
                .Group(RealtimeGroups.User(userId))
                .SendAsync(NotificationReceivedMethod, notification, cancellationToken),
            "notification received",
            notification.EntityId);
    }

    public Task PublishOrderUpdatedAsync(
        OrderUpdatedRealtimeEvent orderUpdated,
        CancellationToken cancellationToken = default)
    {
        var payload = new OrderUpdatedRealtimeDto
        {
            OrderId = orderUpdated.OrderId,
            Status = orderUpdated.Status,
            LinkUrl = orderUpdated.LinkUrl,
            UpdatedAtUtc = orderUpdated.UpdatedAtUtc
        };

        var sends = new List<Task>
        {
            _hubContext.Clients
                .Group(RealtimeGroups.User(orderUpdated.UserId))
                .SendAsync(OrderUpdatedMethod, payload, cancellationToken),
            _hubContext.Clients
                .Group(RealtimeGroups.Order(orderUpdated.OrderId))
                .SendAsync(OrderUpdatedMethod, payload, cancellationToken)
        };

        if (orderUpdated.AgentId.HasValue)
        {
            sends.Add(_hubContext.Clients
                .Group(RealtimeGroups.Agent(orderUpdated.AgentId.Value))
                .SendAsync(OrderUpdatedMethod, payload, cancellationToken));
        }

        if (orderUpdated.NotifyAdmins)
        {
            sends.Add(_hubContext.Clients
                .Group(RealtimeGroups.Admins)
                .SendAsync(OrderUpdatedMethod, payload, cancellationToken));
        }

        return SendAsync(
            () => Task.WhenAll(sends),
            "order updated",
            orderUpdated.OrderId);
    }

    public Task PublishOrderAssignedAsync(
        OrderAssignedRealtimeEvent orderAssigned,
        CancellationToken cancellationToken = default)
    {
        var payload = new OrderAssignedRealtimeDto
        {
            OrderId = orderAssigned.OrderId,
            LinkUrl = orderAssigned.LinkUrl,
            AssignedAtUtc = orderAssigned.AssignedAtUtc
        };

        return SendAsync(
            () => Task.WhenAll(
                _hubContext.Clients
                    .Group(RealtimeGroups.User(orderAssigned.UserId))
                    .SendAsync(OrderAssignedMethod, payload, cancellationToken),
                _hubContext.Clients
                    .Group(RealtimeGroups.Agent(orderAssigned.AgentId))
                    .SendAsync(OrderAssignedMethod, payload, cancellationToken),
                _hubContext.Clients
                    .Group(RealtimeGroups.Admins)
                    .SendAsync(OrderAssignedMethod, payload, cancellationToken),
                _hubContext.Clients
                    .Group(RealtimeGroups.Order(orderAssigned.OrderId))
                    .SendAsync(OrderAssignedMethod, payload, cancellationToken)),
            "order assigned",
            orderAssigned.OrderId);
    }

    public Task PublishCustomerActionChangedAsync(
        CustomerActionChangedRealtimeEvent customerActionChanged,
        CancellationToken cancellationToken = default)
    {
        var payload = new CustomerActionChangedRealtimeDto
        {
            OrderId = customerActionChanged.OrderId,
            RequestId = customerActionChanged.RequestId,
            LinkUrl = customerActionChanged.LinkUrl,
            UpdatedAtUtc = customerActionChanged.UpdatedAtUtc
        };

        var sends = new List<Task>
        {
            _hubContext.Clients
                .Group(RealtimeGroups.User(customerActionChanged.UserId))
                .SendAsync(CustomerActionChangedMethod, payload, cancellationToken),
            _hubContext.Clients
                .Group(RealtimeGroups.Order(customerActionChanged.OrderId))
                .SendAsync(CustomerActionChangedMethod, payload, cancellationToken)
        };

        if (customerActionChanged.AgentId.HasValue)
        {
            sends.Add(_hubContext.Clients
                .Group(RealtimeGroups.Agent(customerActionChanged.AgentId.Value))
                .SendAsync(CustomerActionChangedMethod, payload, cancellationToken));
        }

        return SendAsync(
            () => Task.WhenAll(sends),
            "customer action changed",
            customerActionChanged.OrderId);
    }

    public Task PublishTicketUpdatedAsync(
        TicketUpdatedRealtimeEvent ticketUpdated,
        CancellationToken cancellationToken = default)
    {
        var payload = new TicketUpdatedRealtimeDto
        {
            TicketId = ticketUpdated.TicketId,
            LinkUrl = ticketUpdated.LinkUrl,
            UpdatedAtUtc = ticketUpdated.UpdatedAtUtc
        };

        var sends = new List<Task>();
        if (ticketUpdated.UserId.HasValue)
        {
            sends.Add(_hubContext.Clients
                .Group(RealtimeGroups.User(ticketUpdated.UserId.Value))
                .SendAsync(TicketUpdatedMethod, payload, cancellationToken));
        }

        if (ticketUpdated.NotifyAdmins)
        {
            sends.Add(_hubContext.Clients
                .Group(RealtimeGroups.Admins)
                .SendAsync(TicketUpdatedMethod, payload, cancellationToken));
        }

        return SendAsync(
            () => Task.WhenAll(sends),
            "ticket updated",
            ticketUpdated.TicketId);
    }

    private async Task SendAsync(
        Func<Task> send,
        string eventName,
        Guid entityId)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to send realtime {EventName} event for entity {EntityId}.",
                eventName,
                entityId);
        }
    }
}
