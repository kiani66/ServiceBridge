using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Tests.Inventory;
using Xunit;
using Xunit.Abstractions;

namespace ServiceBridge.Tests.Fulfillment;

public sealed class FulfillmentPolicyResolverSqlServerTests :
    IClassFixture<InventoryAllocationSqlServerFixture>
{
    private readonly InventoryAllocationSqlServerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public FulfillmentPolicyResolverSqlServerTests(
        InventoryAllocationSqlServerFixture fixture,
        ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [SqlServerInventoryAllocationFact]
    public async Task ResolveLatestAsync_UsesOneShapedQueryAndReturnsHighestVersion()
    {
        var serviceVariant = CreateServiceVariant();
        var first = CreatePolicy(serviceVariant.Id, 1);
        var latest = CreatePolicy(serviceVariant.Id, 3);

        await using (var seedContext = _fixture.CreateContext())
        {
            seedContext.ServiceVariants.Add(serviceVariant);
            seedContext.FulfillmentPolicies.AddRange(first, latest);
            await seedContext.SaveChangesAsync();
        }

        var interceptor = new PolicyQueryInterceptor();
        await using var queryContext = _fixture.CreateContext(interceptor);

        var result = await new FulfillmentPolicyResolver(queryContext)
            .ResolveLatestAsync(serviceVariant.Id);

        Assert.NotNull(result);
        Assert.Equal(latest.Id, result.PolicyId);
        Assert.Equal(3, result.Version);
        Assert.Equal(1, interceptor.PolicyQueryCount);
        Assert.Contains("TOP(1)", interceptor.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ORDER BY", interceptor.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("JOIN", interceptor.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Providers", interceptor.CommandText, StringComparison.OrdinalIgnoreCase);
        _output.WriteLine(interceptor.CommandText);
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

    private static ServiceVariant CreateServiceVariant()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var category = new ServiceCategory
        {
            Name = $"Resolver category {suffix}",
            Slug = $"resolver-category-{suffix}",
            IsActive = true
        };
        var provider = new Provider
        {
            Name = $"Resolver provider {suffix}",
            Slug = $"resolver-provider-{suffix}",
            IsActive = true
        };
        var service = new DigitalService
        {
            CategoryId = category.Id,
            Category = category,
            ProviderId = provider.Id,
            Provider = provider,
            Name = $"Resolver service {suffix}",
            Slug = $"resolver-service-{suffix}",
            CurrencyCode = "USD",
            BasePrice = 1m,
            IsActive = true
        };

        return new ServiceVariant
        {
            ServiceId = service.Id,
            Service = service,
            Name = $"Resolver variant {suffix}",
            Slug = $"resolver-variant-{suffix}",
            CurrencyCode = "USD",
            RegionCode = "GLOBAL",
            FulfillmentMode = FulfillmentMode.ManualAgent,
            BasePrice = 1m,
            IsActive = true
        };
    }

    private sealed class PolicyQueryInterceptor : DbCommandInterceptor
    {
        public int PolicyQueryCount { get; private set; }
        public string CommandText { get; private set; } = string.Empty;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains(
                    "FROM [FulfillmentPolicies]",
                    StringComparison.OrdinalIgnoreCase))
            {
                PolicyQueryCount++;
                CommandText = command.CommandText;
            }

            return base.ReaderExecutingAsync(
                command,
                eventData,
                result,
                cancellationToken);
        }
    }
}
