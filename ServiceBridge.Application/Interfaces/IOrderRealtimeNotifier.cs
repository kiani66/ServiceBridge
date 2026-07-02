using ServiceBridge.Application.DTOs;

namespace ServiceBridge.Application.Interfaces;

public interface IOrderRealtimeNotifier
{
    Task NotifyOrderUpdatedAsync(
        OrderUpdatedRealtimeMessage message,
        CancellationToken cancellationToken = default);
}
