using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceBridge.Infrastructure.Persistence;

namespace ServiceBridge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceBridgeInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        if (configureDbContext is not null)
        {
            services.AddDbContext<ServiceBridgeDbContext>(configureDbContext);
        }

        return services;
    }
}
