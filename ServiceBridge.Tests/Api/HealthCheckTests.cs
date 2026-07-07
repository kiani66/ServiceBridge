using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ServiceBridge.Api.HealthChecks;
using ServiceBridge.Infrastructure.Data;
using Xunit;

namespace ServiceBridge.Tests.Api;

public class HealthCheckTests
{
    [Fact]
    public void AddServiceBridgeHealthChecks_RegistersSelfAndDatabaseChecks()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ServiceBridgeDbContext>(options =>
            options.UseSqlServer("Server=.;Database=ServiceBridgeHealthTests;Trusted_Connection=True;"));
        services.AddServiceBridgeHealthChecks();

        using var provider = services.BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value;

        Assert.Contains(options.Registrations, registration =>
            registration.Name == "self" &&
            registration.Tags.Contains("live"));
        Assert.Contains(options.Registrations, registration =>
            registration.Name == "database" &&
            registration.Tags.Contains("ready"));
    }

    [Fact]
    public async Task DatabaseHealthCheck_WhenContextCannotCheckConnection_ReturnsUnhealthy()
    {
        var context = new ServiceBridgeDbContext(
            new DbContextOptionsBuilder<ServiceBridgeDbContext>().Options);
        var healthCheck = new DatabaseHealthCheck(context);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Database connectivity check failed.", result.Description);
    }
}
