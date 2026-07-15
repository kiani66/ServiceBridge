using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceBridge.Api.Controllers;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;
using ServiceBridge.Infrastructure.Repositories;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.AdminOrders;

public class PendingInventoryFulfillmentTests
{
    [Fact]
    public async Task PendingQuery_ClassifiesOperationalStatesAndExcludesManualAndFulfilled()
    {
        await using var context = CreateContext();
        var setup = await SeedAsync(context);
        var repository = CreateRepository(context);

        var result = await repository.GetPendingInventoryFulfillmentAsync(1, 20);

        Assert.Equal(3, result.TotalCount);
        Assert.DoesNotContain(result.Items, x => x.OrderId == setup.ManualOrder.Id);
        Assert.DoesNotContain(result.Items, x => x.OrderId == setup.FulfilledOrder.Id);

        var awaiting = Assert.Single(result.Items, x => x.OrderId == setup.AwaitingOrder.Id);
        Assert.Equal(AdminInventoryFulfillmentState.AwaitingStock,
            awaiting.InventoryFulfillmentState);
        Assert.True(awaiting.CanRetryInventoryFulfillment);
        Assert.Null(awaiting.InventoryAllocationStatus);

        var recoverable = Assert.Single(result.Items,
            x => x.OrderId == setup.RecoverableOrder.Id);
        Assert.Equal(AdminInventoryFulfillmentState.RecoverableAllocation,
            recoverable.InventoryFulfillmentState);
        Assert.True(recoverable.CanRetryInventoryFulfillment);
        Assert.Equal(nameof(InventoryItemStatus.Reserved),
            recoverable.InventoryAllocationStatus);

        var inconsistent = Assert.Single(result.Items,
            x => x.OrderId == setup.InconsistentOrder.Id);
        Assert.Equal(AdminInventoryFulfillmentState.Inconsistent,
            inconsistent.InventoryFulfillmentState);
        Assert.False(inconsistent.CanRetryInventoryFulfillment);
        Assert.Equal(nameof(InventoryItemStatus.Delivered),
            inconsistent.InventoryAllocationStatus);
    }

    [Fact]
    public async Task PendingQuery_UsesLatestPaymentCreatedAtThenIdDeterministically()
    {
        await using var context = CreateContext();
        var setup = await SeedAsync(context);
        var repository = CreateRepository(context);

        await AddPaymentAsync(
            context,
            setup.AwaitingOrder,
            PaymentStatus.Pending,
            setup.Now.AddMinutes(1),
            Guid.Parse("00000000-0000-0000-0000-000000000100"));
        await AddPaymentAsync(
            context,
            setup.RecoverableOrder,
            PaymentStatus.Pending,
            setup.Now,
            Guid.Parse("00000000-0000-0000-0000-000000000020"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await repository.GetPendingInventoryFulfillmentAsync(1, 20);

        Assert.DoesNotContain(result.Items, x => x.OrderId == setup.AwaitingOrder.Id);
        Assert.DoesNotContain(result.Items, x => x.OrderId == setup.RecoverableOrder.Id);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(setup.InconsistentOrder.Id, Assert.Single(result.Items).OrderId);
    }

    [Fact]
    public async Task PendingQuery_UsesStableOldestFirstPagingAndDoesNotMutateData()
    {
        await using var context = CreateContext();
        var setup = await SeedAsync(context);
        var repository = CreateRepository(context);

        var firstPage = await repository.GetPendingInventoryFulfillmentAsync(1, 2);
        var secondPage = await repository.GetPendingInventoryFulfillmentAsync(2, 2);

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(
            [setup.AwaitingOrder.Id, setup.RecoverableOrder.Id],
            firstPage.Items.Select(x => x.OrderId));
        Assert.Equal(setup.InconsistentOrder.Id, Assert.Single(secondPage.Items).OrderId);
        Assert.DoesNotContain(context.ChangeTracker.Entries(),
            entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
    }

    [Fact]
    public async Task AdminOrderDetail_ExposesSafeInventoryFulfillmentVisibilityWithoutSecrets()
    {
        await using var context = CreateContext();
        var setup = await SeedAsync(context);
        var repository = CreateRepository(context);

        var inventoryDetail = await repository.GetOrderDetailAsync(Guid.NewGuid(),
            setup.RecoverableOrder.Id);
        var manualDetail = await repository.GetOrderDetailAsync(Guid.NewGuid(),
            setup.ManualOrder.Id);

        Assert.NotNull(inventoryDetail);
        Assert.Equal(setup.RecoverableOrder.OrderNumber, inventoryDetail.OrderNumber);
        Assert.Equal(nameof(FulfillmentMode.InventoryCode),
            inventoryDetail.ServiceFulfillmentMode);
        Assert.Equal(AdminInventoryFulfillmentState.RecoverableAllocation,
            inventoryDetail.InventoryFulfillmentState);
        Assert.Equal(nameof(InventoryItemStatus.Reserved),
            inventoryDetail.InventoryAllocationStatus);
        Assert.True(inventoryDetail.CanRetryInventoryFulfillment);

        Assert.NotNull(manualDetail);
        Assert.Equal(nameof(FulfillmentMode.ManualAgent),
            manualDetail.ServiceFulfillmentMode);
        Assert.Equal(AdminInventoryFulfillmentState.NotApplicable,
            manualDetail.InventoryFulfillmentState);
        Assert.False(manualDetail.CanRetryInventoryFulfillment);

        var properties = typeof(AdminPendingInventoryFulfillmentDto).GetProperties()
            .Concat(typeof(AdminOrderDetailDto).GetProperties()
                .Where(property => property.Name.StartsWith("Inventory", StringComparison.Ordinal) ||
                    property.Name is nameof(AdminOrderDetailDto.ServiceFulfillmentMode) ||
                    property.Name is nameof(AdminOrderDetailDto.CanRetryInventoryFulfillment)))
            .Select(property => property.Name)
            .ToArray();
        Assert.DoesNotContain(properties, property =>
            property.Contains("Protected", StringComparison.OrdinalIgnoreCase) ||
            property.Contains("Fingerprint", StringComparison.OrdinalIgnoreCase) ||
            property.Contains("Code", StringComparison.OrdinalIgnoreCase) ||
            property.Contains("Gateway", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PendingController_DelegatesToQueryServiceAndUsesAdminRouteConvention()
    {
        var queryService = new FakeQueryService();
        var controller = new AdminOrdersController(queryService, null!, null!);

        var actionResult = await controller.GetPendingInventoryFulfillment(2, 7);

        var response = Assert.IsType<ObjectResult>(actionResult);
        var result = Assert.IsType<ServiceResult<PagedResultDto<AdminPendingInventoryFulfillmentDto>>>(
            response.Value);
        Assert.True(result.Success);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal(1, queryService.PendingCallCount);
        Assert.Equal((2, 7), queryService.LastPendingArguments);
    }

    private static AdminOrderQueryRepository CreateRepository(
        ServiceBridgeDbContext context)
        => new(context, new FakeOrderFieldProtector());

    private static ServiceBridgeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ServiceBridgeDbContext(options);
    }

    private static async Task<Scenario> SeedAsync(ServiceBridgeDbContext context)
    {
        var now = new DateTime(2026, 7, 15, 8, 0, 0, DateTimeKind.Utc);
        var user = new User { FullName = "Operations User", Email = "ops@example.test" };
        var provider = new Provider { Name = "Operations Provider", Slug = "operations-provider" };
        var service = new DigitalService
        {
            Name = "Operations Service",
            Slug = "operations-service",
            Provider = provider,
            ProviderId = provider.Id
        };
        var variant = new ServiceVariant
        {
            Service = service,
            ServiceId = service.Id,
            Name = "Operations Variant",
            Slug = "operations-variant",
            FulfillmentMode = FulfillmentMode.InventoryCode
        };
        var manualOrder = CreateOrder(user, service, variant, "MANUAL", FulfillmentMode.ManualAgent,
            OrderStatus.Paid, now.AddMinutes(-5));
        var awaitingOrder = CreateOrder(user, service, variant, "AWAITING", FulfillmentMode.InventoryCode,
            OrderStatus.Paid, now.AddMinutes(-4));
        var recoverableOrder = CreateOrder(user, service, variant, "RECOVERABLE", FulfillmentMode.InventoryCode,
            OrderStatus.Processing, now.AddMinutes(-3));
        var fulfilledOrder = CreateOrder(user, service, variant, "FULFILLED", FulfillmentMode.InventoryCode,
            OrderStatus.Completed, now.AddMinutes(-2));
        var inconsistentOrder = CreateOrder(user, service, variant, "INCONSISTENT", FulfillmentMode.InventoryCode,
            OrderStatus.Paid, now.AddMinutes(-1));

        context.AddRange(user, provider, service, variant);
        context.Orders.AddRange(
            manualOrder,
            awaitingOrder,
            recoverableOrder,
            fulfilledOrder,
            inconsistentOrder);
        await AddPaymentAsync(context, manualOrder, PaymentStatus.Completed, now,
            Guid.Parse("00000000-0000-0000-0000-000000000001"));
        await AddPaymentAsync(context, awaitingOrder, PaymentStatus.Completed, now,
            Guid.Parse("00000000-0000-0000-0000-000000000002"));
        await AddPaymentAsync(context, recoverableOrder, PaymentStatus.Completed, now,
            Guid.Parse("00000000-0000-0000-0000-000000000003"));
        await AddPaymentAsync(context, fulfilledOrder, PaymentStatus.Completed, now,
            Guid.Parse("00000000-0000-0000-0000-000000000004"));
        await AddPaymentAsync(context, inconsistentOrder, PaymentStatus.Completed, now,
            Guid.Parse("00000000-0000-0000-0000-000000000005"));
        context.InventoryItems.Add(CreateAllocation(variant.Id, recoverableOrder.Id,
            now, delivered: false));
        context.InventoryItems.Add(CreateAllocation(variant.Id, fulfilledOrder.Id,
            now, delivered: true));
        context.InventoryItems.Add(CreateAllocation(variant.Id, inconsistentOrder.Id,
            now, delivered: true));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return new Scenario(now, manualOrder, awaitingOrder, recoverableOrder,
            fulfilledOrder, inconsistentOrder);
    }

    private static Order CreateOrder(
        User user,
        DigitalService service,
        ServiceVariant variant,
        string suffix,
        FulfillmentMode fulfillmentMode,
        OrderStatus status,
        DateTime createdAt)
    {
        return new Order
        {
            OrderNumber = $"ORD-{suffix}",
            UserId = user.Id,
            User = user,
            ServiceId = service.Id,
            Service = service,
            ServiceVariantId = variant.Id,
            ServiceVariant = variant,
            ServiceFulfillmentModeSnapshot = fulfillmentMode,
            Status = status,
            CreatedAt = createdAt,
            PaidAt = createdAt
        };
    }

    private static async Task AddPaymentAsync(
        ServiceBridgeDbContext context,
        Order order,
        PaymentStatus status,
        DateTime createdAt,
        Guid? paymentId = null)
    {
        var payment = new Payment
        {
            Id = paymentId ?? Guid.NewGuid(),
            OrderId = order.Id,
            CreatedAt = createdAt
        };
        if (status == PaymentStatus.Completed)
        {
            payment.Complete("reference", null, createdAt);
        }

        context.Payments.Add(payment);
        await Task.CompletedTask;
    }

    private static InventoryItem CreateAllocation(
        Guid variantId,
        Guid orderId,
        DateTime now,
        bool delivered)
    {
        var item = InventoryItem.Create(
            variantId,
            FulfillmentMode.InventoryCode,
            "protected-value",
            "fingerprint-value",
            Guid.NewGuid(),
            now.AddMinutes(-10));
        item.ReserveFor(orderId, now.AddMinutes(-5));
        if (delivered)
        {
            item.MarkDelivered(orderId, now.AddMinutes(-4));
        }

        return item;
    }

    private sealed record Scenario(
        DateTime Now,
        Order ManualOrder,
        Order AwaitingOrder,
        Order RecoverableOrder,
        Order FulfilledOrder,
        Order InconsistentOrder);

    private sealed class FakeQueryService : IAdminOrderQueryService
    {
        public int PendingCallCount { get; private set; }
        public (int Page, int PageSize) LastPendingArguments { get; private set; }

        public Task<ServiceResult<PagedResultDto<AdminPendingInventoryFulfillmentDto>>>
            GetPendingInventoryFulfillmentAsync(int page, int pageSize)
        {
            PendingCallCount++;
            LastPendingArguments = (page, pageSize);
            return Task.FromResult(
                ServiceResult<PagedResultDto<AdminPendingInventoryFulfillmentDto>>.Ok(
                    new PagedResultDto<AdminPendingInventoryFulfillmentDto>()));
        }

        public Task<ServiceResult<PagedResultDto<AdminOrderListItemDto>>> GetOrdersAsync(int page, int pageSize, OrderStatus? status, string? search, Guid? assignedAgentId) => throw new NotImplementedException();
        public Task<ServiceResult<PagedResultDto<AdminAssignableOrderDto>>> GetPaidUnassignedOrdersAsync(int page, int pageSize) => throw new NotImplementedException();
        public Task<ServiceResult<IReadOnlyCollection<AgentSummaryDto>>> GetActiveAgentsAsync() => throw new NotImplementedException();
        public Task<ServiceResult<AdminOrderDashboardSummaryDto>> GetDashboardSummaryAsync() => throw new NotImplementedException();
        public Task<ServiceResult<AdminOrderDetailDto>> GetOrderDetailAsync(Guid adminId, Guid orderId) => throw new NotImplementedException();
        public Task<ServiceResult<AdminOrderCredentialsDto>> RevealOrderCredentialsAsync(Guid adminId, Guid orderId) => throw new NotImplementedException();
        public Task<ServiceResult<PagedResultDto<AdminPaymentListItemDto>>> GetPaymentsAsync(int page, int pageSize, PaymentStatus? status, string? search) => throw new NotImplementedException();
        public Task<ServiceResult<PagedResultDto<AdminAuditEventDto>>> GetAuditEventsAsync(AdminAuditQueryDto query) => throw new NotImplementedException();
        public Task<ServiceResult<IReadOnlyCollection<AdminOrderTimelineEventDto>>> GetOrderTimelineAsync(Guid orderId) => throw new NotImplementedException();
    }
}
