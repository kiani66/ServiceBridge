using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Orders;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Services;

public sealed class FulfillmentPolicyResolver : IFulfillmentPolicyResolver
{
    private readonly ServiceBridgeDbContext _context;

    public FulfillmentPolicyResolver(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public async Task<OrderFulfillmentPolicySnapshot?> ResolveLatestAsync(
        Guid serviceVariantId)
    {
        var policy = await _context.FulfillmentPolicies
            .AsNoTracking()
            .Where(candidate => candidate.ServiceVariantId == serviceVariantId)
            .OrderByDescending(candidate => candidate.Version)
            .Select(candidate => new
            {
                PolicyId = candidate.Id,
                candidate.Version,
                candidate.ProcurementStrategy,
                candidate.FulfillmentMode,
                candidate.DeliveryArtifactType
            })
            .FirstOrDefaultAsync();

        return policy == null
            ? null
            : OrderFulfillmentPolicySnapshot.Create(
                policy.PolicyId,
                policy.Version,
                policy.ProcurementStrategy,
                policy.FulfillmentMode,
                policy.DeliveryArtifactType);
    }
}
