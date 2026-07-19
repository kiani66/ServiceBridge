using ServiceBridge.Application.Orders;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Orders;

public class CancelMyOrderCommandTests
{
    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.WaitingForPayment)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Assigned)]
    public async Task ExecuteAsync_BeforeProcessing_CancelsAndWritesStatusLog(
        OrderStatus initialStatus)
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, initialStatus);
        var repository = new FakeUserOrderCommandRepository { Order = order };
        var command = new CancelMyOrderCommand(repository);

        var result = await command.ExecuteAsync(userId, order.Id);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.NotNull(order.CancelledAt);
        Assert.Equal(1, repository.SaveCount);

        var log = Assert.Single(order.StatusHistory);
        Assert.Equal(initialStatus, log.PreviousStatus);
        Assert.Equal(OrderStatus.Cancelled, log.NewStatus);
        Assert.Equal(userId, log.ChangedByUserId);
    }

    [Theory]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Failed)]
    public async Task ExecuteAsync_WhenStateBlocksCancellation_ReturnsConflict(
        OrderStatus initialStatus)
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, initialStatus);
        var repository = new FakeUserOrderCommandRepository { Order = order };
        var command = new CancelMyOrderCommand(repository);

        var result = await command.ExecuteAsync(userId, order.Id);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
        Assert.Equal(initialStatus, order.Status);
        Assert.Empty(order.StatusHistory);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task ExecuteAsync_ForAnotherUsersOrder_ReturnsNotFound()
    {
        var order = CreateOrder(Guid.NewGuid(), OrderStatus.WaitingForPayment);
        var repository = new FakeUserOrderCommandRepository { Order = order };
        var command = new CancelMyOrderCommand(repository);

        var result = await command.ExecuteAsync(Guid.NewGuid(), order.Id);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrderChangesConcurrently_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, OrderStatus.WaitingForPayment);
        var repository = new FakeUserOrderCommandRepository
        {
            Order = order,
            SaveSucceeds = false
        };
        var command = new CancelMyOrderCommand(repository);

        var result = await command.ExecuteAsync(userId, order.Id);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
        Assert.Equal(1, repository.SaveCount);
    }

    private static Order CreateOrder(Guid userId, OrderStatus status)
    {
        var order = OrderTestFactory.Create();
        order.Id = Guid.NewGuid();
        order.UserId = userId;
        order.Status = status;
        return order;
    }
}
