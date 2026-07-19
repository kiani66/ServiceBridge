using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Orders;

namespace ServiceBridge.Tests.TestDoubles;

internal sealed class FakeFulfillmentPolicyResolver : IFulfillmentPolicyResolver
{
    public OrderFulfillmentPolicySnapshot? Snapshot { get; set; }
    public Guid? RequestedServiceVariantId { get; private set; }
    public int CallCount { get; private set; }

    public Task<OrderFulfillmentPolicySnapshot?> ResolveLatestAsync(
        Guid serviceVariantId)
    {
        RequestedServiceVariantId = serviceVariantId;
        CallCount++;
        return Task.FromResult(Snapshot);
    }
}
