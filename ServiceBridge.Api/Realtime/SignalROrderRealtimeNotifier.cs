using Microsoft.AspNetCore.SignalR;
using ServiceBridge.Api.Hubs;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Realtime;

public sealed class SignalROrderRealtimeNotifier : IOrderRealtimeNotifier
{
    private const string ClientMethodName = "orderUpdated";

    private readonly IHubContext<OrderUpdatesHub> _hubContext;
    private readonly ILogger<SignalROrderRealtimeNotifier> _logger;

    public SignalROrderRealtimeNotifier(
        IHubContext<OrderUpdatesHub> hubContext,
        ILogger<SignalROrderRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyOrderUpdatedAsync(
        OrderUpdatedRealtimeMessage message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sends = new List<Task>
            {
                _hubContext.Clients
                    .Group($"user:{message.UserId}")
                    .SendAsync(ClientMethodName, message, cancellationToken),
                _hubContext.Clients
                    .Group($"role:{AppConstants.Roles.Admin}")
                    .SendAsync(ClientMethodName, message, cancellationToken)
            };

            if (message.AgentId.HasValue)
            {
                sends.Add(_hubContext.Clients
                    .Group($"agent:{message.AgentId.Value}")
                    .SendAsync(ClientMethodName, message, cancellationToken));
            }

            await Task.WhenAll(sends);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to send realtime order update for order {OrderId}.",
                message.OrderId);
        }
    }
}
