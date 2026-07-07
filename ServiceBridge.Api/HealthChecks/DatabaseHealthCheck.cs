using Microsoft.Extensions.Diagnostics.HealthChecks;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Api.HealthChecks;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly ServiceBridgeDbContext _context;

    public DatabaseHealthCheck(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database connectivity check failed.");
        }
        catch
        {
            return HealthCheckResult.Unhealthy("Database connectivity check failed.");
        }
    }
}
