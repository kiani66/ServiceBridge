using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ServiceBridge.Infrastructure.Data;
using ServiceBridge.Infrastructure.Services;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class DevelopmentDataSeederTests
{
    [Fact]
    public async Task SeedAsync_WhenDisabled_DoesNothing()
    {
        await using var context = CreateContext();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DevelopmentSeed:Enabled"] = "false"
            })
            .Build();
        var seeder = new DevelopmentDataSeeder(
            context,
            new BcryptPasswordHasher(),
            configuration);

        await seeder.SeedAsync();

        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task SeedAsync_WhenEnabledConfigurationIsInvalid_Throws()
    {
        await using var context = CreateContext();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DevelopmentSeed:Enabled"] = "true",
                ["DevelopmentSeed:UsdRateToIrr"] = "0"
            })
            .Build();
        var seeder = new DevelopmentDataSeeder(
            context,
            new BcryptPasswordHasher(),
            configuration);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => seeder.SeedAsync());

        Assert.Equal(
            "DevelopmentSeed configuration is invalid.",
            exception.Message);
    }

    private static ServiceBridgeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;" +
                "Database=ServiceBridgeDevelopmentSeedTests")
            .Options;
        return new ServiceBridgeDbContext(options);
    }
}
