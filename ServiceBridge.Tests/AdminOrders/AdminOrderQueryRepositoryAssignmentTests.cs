using Microsoft.EntityFrameworkCore;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;
using ServiceBridge.Infrastructure.Repositories;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.AdminOrders;

public class AdminOrderQueryRepositoryAssignmentTests
{
    [Fact]
    public async Task PaidUnassignedOrders_IncludeManualAgentAndExcludeInventoryCode()
    {
        await using var context = CreateContext();
        var (manualOrder, inventoryOrder) = await SeedPaidOrdersAsync(context);
        var repository = new AdminOrderQueryRepository(
            context,
            new FakeOrderFieldProtector());

        var result = await repository.GetPaidUnassignedOrdersAsync(1, 20);

        var item = Assert.Single(result.Items);
        Assert.Equal(manualOrder.Id, item.OrderId);
        Assert.DoesNotContain(result.Items, x => x.OrderId == inventoryOrder.Id);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GeneralAdminOrders_StillIncludeInventoryCodeOrders()
    {
        await using var context = CreateContext();
        var (manualOrder, inventoryOrder) = await SeedPaidOrdersAsync(context);
        var repository = new AdminOrderQueryRepository(
            context,
            new FakeOrderFieldProtector());

        var result = await repository.GetOrdersAsync(1, 20, null, null, null);

        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, x => x.OrderId == manualOrder.Id);
        Assert.Contains(result.Items, x => x.OrderId == inventoryOrder.Id);
    }

    [Fact]
    public async Task DashboardUnassignedCount_IncludesOnlyManualAgentOrders()
    {
        await using var context = CreateContext();
        await SeedPaidOrdersAsync(context);
        var repository = new AdminOrderQueryRepository(
            context,
            new FakeOrderFieldProtector());

        var result = await repository.GetDashboardSummaryAsync();

        Assert.Equal(2, result.PaidOrderCount);
        Assert.Equal(1, result.UnassignedOrderCount);
    }

    private static ServiceBridgeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ServiceBridgeDbContext(options);
    }

    private static async Task<(Order ManualOrder, Order InventoryOrder)>
        SeedPaidOrdersAsync(ServiceBridgeDbContext context)
    {
        var user = new User
        {
            FullName = "Assignment Customer",
            Email = "assignment@example.test"
        };
        var service = new DigitalService
        {
            Name = "Assignment Service",
            Slug = $"assignment-{Guid.NewGuid():N}"
        };
        var manualOrder = CreatePaidOrder(
            user,
            service,
            "ORD-MANUAL",
            FulfillmentMode.ManualAgent);
        var inventoryOrder = CreatePaidOrder(
            user,
            service,
            "ORD-INVENTORY",
            FulfillmentMode.InventoryCode);

        context.Users.Add(user);
        context.DigitalServices.Add(service);
        context.Orders.AddRange(manualOrder, inventoryOrder);
        await context.SaveChangesAsync();

        return (manualOrder, inventoryOrder);
    }

    private static Order CreatePaidOrder(
        User user,
        DigitalService service,
        string orderNumber,
        FulfillmentMode fulfillmentMode)
    {
        return new Order
        {
            OrderNumber = orderNumber,
            UserId = user.Id,
            User = user,
            ServiceId = service.Id,
            Service = service,
            Status = OrderStatus.Paid,
            PaidAt = DateTime.UtcNow,
            ServiceFulfillmentModeSnapshot = fulfillmentMode
        };
    }
}
