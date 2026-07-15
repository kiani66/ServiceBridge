using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Infrastructure.Data;
using ServiceBridge.Infrastructure.Repositories;
using Xunit;

namespace ServiceBridge.Tests.Inventory;

public class InventoryCodeFulfillmentRepositoryUnitTests
{
    [Fact]
    public async Task LockNextAvailableAsync_WithoutActiveTransaction_IsRejectedBeforeDatabaseAccess()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=UnusedInventoryAllocationGuard")
            .Options;
        await using var context = new ServiceBridgeDbContext(options);
        var repository = new InventoryCodeFulfillmentRepository(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.LockNextAvailableAsync(Guid.NewGuid()));

        Assert.Equal(
            "LockNextAvailableAsync requires an active database transaction.",
            exception.Message);
    }

    [Fact]
    public void Repository_ImplementsContractAndHasNoCodeProtectorDependency()
    {
        Assert.Contains(
            typeof(IInventoryCodeFulfillmentRepository),
            typeof(InventoryCodeFulfillmentRepository).GetInterfaces());

        var constructor = Assert.Single(
            typeof(InventoryCodeFulfillmentRepository).GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal(typeof(ServiceBridgeDbContext), parameter.ParameterType);
    }
}
