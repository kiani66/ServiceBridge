using Microsoft.EntityFrameworkCore;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;
using ServiceBridge.Infrastructure.Services;
using Xunit;

namespace ServiceBridge.Tests.Fulfillment;

public sealed class FulfillmentPolicyResolverTests
{
    [Fact]
    public async Task ResolveLatestAsync_ReturnsHighestVersionForRequestedVariant()
    {
        var variantId = Guid.NewGuid();
        var expected = CreatePolicy(variantId, 4);
        await using var context = CreateContext();
        context.FulfillmentPolicies.AddRange(
            CreatePolicy(variantId, 1),
            expected,
            CreatePolicy(Guid.NewGuid(), 9));
        await context.SaveChangesAsync();

        var result = await new FulfillmentPolicyResolver(context)
            .ResolveLatestAsync(variantId);

        Assert.NotNull(result);
        Assert.Equal(expected.Id, result.PolicyId);
        Assert.Equal(4, result.Version);
        Assert.Equal(expected.ProcurementStrategy, result.ProcurementStrategy);
        Assert.Equal(expected.FulfillmentMode, result.FulfillmentMode);
        Assert.Equal(expected.DeliveryArtifactType, result.DeliveryArtifactType);
    }

    [Fact]
    public async Task ResolveLatestAsync_WhenPolicyDoesNotExist_ReturnsNull()
    {
        await using var context = CreateContext();

        var result = await new FulfillmentPolicyResolver(context)
            .ResolveLatestAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    private static ServiceBridgeDbContext CreateContext()
    {
        return new ServiceBridgeDbContext(
            new DbContextOptionsBuilder<ServiceBridgeDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private static FulfillmentPolicy CreatePolicy(
        Guid serviceVariantId,
        int version)
    {
        return FulfillmentPolicy.Create(
            serviceVariantId,
            version,
            ProcurementStrategy.OnDemandManual,
            FulfillmentMode.ManualAgent,
            DeliveryArtifactType.AccountAccess);
    }
}
