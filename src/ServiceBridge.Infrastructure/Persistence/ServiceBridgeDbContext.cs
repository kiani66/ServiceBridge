using Microsoft.EntityFrameworkCore;

namespace ServiceBridge.Infrastructure.Persistence;

public sealed class ServiceBridgeDbContext(DbContextOptions<ServiceBridgeDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServiceBridgeDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
