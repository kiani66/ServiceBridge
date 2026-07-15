using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ServiceBridge.Infrastructure.Data;
using Xunit;

namespace ServiceBridge.Tests.Inventory;

public sealed class InventoryAllocationSqlServerFixture : IAsyncLifetime
{
    public const string ConnectionEnvironmentVariable =
        "SERVICEBRIDGE_TEST_SQLSERVER_CONNECTION";

    private readonly string? _baseConnectionString =
        Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);
    private string? _databaseName;
    private bool _databaseCreated;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_baseConnectionString);
    public string ConfigurationError =>
        $"Set {ConnectionEnvironmentVariable} to a SQL Server connection string " +
        "whose principal can create and drop temporary databases.";
    public string? DatabaseConnectionString { get; private set; }

    public async Task InitializeAsync()
    {
        if (!IsConfigured)
        {
            return;
        }

        _databaseName = $"ServiceBridgeInventoryAllocation_{Guid.NewGuid():N}";
        var masterConnectionString = BuildConnectionString("master");

        await using (var connection = new SqlConnection(masterConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{_databaseName}]";
            await command.ExecuteNonQueryAsync();
            _databaseCreated = true;
        }

        DatabaseConnectionString = BuildConnectionString(_databaseName);

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public ServiceBridgeDbContext CreateContext(
        params IInterceptor[] interceptors)
    {
        if (DatabaseConnectionString is null)
        {
            throw new InvalidOperationException(ConfigurationError);
        }

        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseSqlServer(DatabaseConnectionString);

        if (interceptors.Length > 0)
        {
            options.AddInterceptors(interceptors);
        }

        return new ServiceBridgeDbContext(options.Options);
    }

    public async Task DisposeAsync()
    {
        if (!_databaseCreated || _databaseName is null)
        {
            return;
        }

        await using var connection = new SqlConnection(BuildConnectionString("master"));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [{_databaseName}];
            """;
        await command.ExecuteNonQueryAsync();
    }

    private string BuildConnectionString(string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(_baseConnectionString)
        {
            InitialCatalog = databaseName,
            Pooling = false
        };

        return builder.ConnectionString;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlServerInventoryAllocationFactAttribute : FactAttribute
{
    public SqlServerInventoryAllocationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
                InventoryAllocationSqlServerFixture.ConnectionEnvironmentVariable)))
        {
            Skip =
                $"Set {InventoryAllocationSqlServerFixture.ConnectionEnvironmentVariable} " +
                "to run the required SQL Server allocation test.";
        }
    }
}
