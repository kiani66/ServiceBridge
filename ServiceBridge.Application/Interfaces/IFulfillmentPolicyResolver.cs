using ServiceBridge.Domain.Orders;

namespace ServiceBridge.Application.Interfaces;

public interface IFulfillmentPolicyResolver
{
    Task<OrderFulfillmentPolicySnapshot?> ResolveLatestAsync(
        Guid serviceVariantId);
}
