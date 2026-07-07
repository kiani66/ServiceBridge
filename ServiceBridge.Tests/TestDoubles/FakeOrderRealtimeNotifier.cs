using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakeOrderRealtimeNotifier : IOrderRealtimeNotifier
{
    public List<OrderUpdatedRealtimeMessage> Messages { get; } = new();

    public Task NotifyOrderUpdatedAsync(
        OrderUpdatedRealtimeMessage message,
        CancellationToken cancellationToken = default)
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}
