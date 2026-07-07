using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakeOrderRealtimeNotifier : IOrderRealtimeNotifier
{
    public List<OrderUpdatedRealtimeMessage> Messages { get; } = new();
    public bool ThrowOnNotify { get; set; }

    public Task NotifyOrderUpdatedAsync(
        OrderUpdatedRealtimeMessage message,
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnNotify)
        {
            throw new InvalidOperationException("Realtime notification failed.");
        }

        Messages.Add(message);
        return Task.CompletedTask;
    }
}
