using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ServiceBridge.Infrastructure.Data;

public class ServiceBridgeDbContextFactory : IDesignTimeDbContextFactory<ServiceBridgeDbContext>
{
    public ServiceBridgeDbContext CreateDbContext(string[] args)
    {
        var apiProjectPath = ResolveApiProjectPath();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ServiceBridgeDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ServiceBridgeDbContext(optionsBuilder.Options);
    }

    private static string ResolveApiProjectPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(currentDirectory, "ServiceBridge.Api"),
            Path.GetFullPath(Path.Combine(currentDirectory, "..", "ServiceBridge.Api")),
            currentDirectory
        };

        var apiProjectPath = candidates.FirstOrDefault(path =>
            File.Exists(Path.Combine(path, "appsettings.json")));

        if (apiProjectPath is null)
        {
            throw new InvalidOperationException("Could not locate ServiceBridge.Api appsettings.json.");
        }

        return apiProjectPath;
    }
}
