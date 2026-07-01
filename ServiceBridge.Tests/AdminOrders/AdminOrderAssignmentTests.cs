using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.AdminOrders;

public class AdminOrderAssignmentTests
{
    [Fact]
    public async Task AssignAsync_WhenPaidAndAgentExists_AssignsAndWritesAdminLog()
    {
        var adminId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var order = CreateOrder(OrderStatus.Paid);
        var repository = new FakeAdminOrderCommandRepository { Order = order };
        var service = new AdminOrderAssignmentService(
            repository,
            new FakeNotificationService());

        var result = await service.AssignAsync(
            adminId,
            order.Id,
            new AssignOrderRequest { AgentId = agentId });

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Assigned, order.Status);
        Assert.Equal(agentId, order.AssignedAgentId);
        Assert.NotNull(order.AssignedAt);
        Assert.Equal(1, repository.SaveCount);

        var log = Assert.Single(order.StatusHistory);
        Assert.Equal(OrderStatus.Paid, log.PreviousStatus);
        Assert.Equal(OrderStatus.Assigned, log.NewStatus);
        Assert.Equal(OrderLogActorType.Admin, log.ActorType);
        Assert.Equal(adminId, log.ActorId);
        Assert.Equal(adminId, log.ChangedByUserId);
    }

    [Fact]
    public async Task AssignAsync_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        var service = new AdminOrderAssignmentService(
            new FakeAdminOrderCommandRepository(),
            new FakeNotificationService());

        var result = await service.AssignAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new AssignOrderRequest { AgentId = Guid.NewGuid() });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task AssignAsync_WhenAgentIsMissing_ReturnsNotFound()
    {
        var order = CreateOrder(OrderStatus.Paid);
        var repository = new FakeAdminOrderCommandRepository
        {
            Order = order,
            AgentExists = false
        };
        var service = new AdminOrderAssignmentService(
            repository,
            new FakeNotificationService());

        var result = await service.AssignAsync(
            Guid.NewGuid(),
            order.Id,
            new AssignOrderRequest { AgentId = Guid.NewGuid() });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(0, repository.SaveCount);
    }

    [Theory]
    [InlineData(OrderStatus.WaitingForPayment)]
    [InlineData(OrderStatus.Assigned)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Completed)]
    public async Task AssignAsync_WhenOrderIsNotPaid_ReturnsConflict(
        OrderStatus status)
    {
        var order = CreateOrder(status);
        var repository = new FakeAdminOrderCommandRepository { Order = order };
        var service = new AdminOrderAssignmentService(
            repository,
            new FakeNotificationService());

        var result = await service.AssignAsync(
            Guid.NewGuid(),
            order.Id,
            new AssignOrderRequest { AgentId = Guid.NewGuid() });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task AssignAsync_WhenOrderChangesConcurrently_ReturnsConflict()
    {
        var order = CreateOrder(OrderStatus.Paid);
        var repository = new FakeAdminOrderCommandRepository
        {
            Order = order,
            SaveSucceeds = false
        };
        var service = new AdminOrderAssignmentService(
            repository,
            new FakeNotificationService());

        var result = await service.AssignAsync(
            Guid.NewGuid(),
            order.Id,
            new AssignOrderRequest { AgentId = Guid.NewGuid() });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task AssignAsync_WhenPaidOrderAlreadyHasAgent_ReturnsConflict()
    {
        var order = CreateOrder(OrderStatus.Paid);
        order.AssignedAgentId = Guid.NewGuid();
        var repository = new FakeAdminOrderCommandRepository { Order = order };
        var service = new AdminOrderAssignmentService(
            repository,
            new FakeNotificationService());

        var result = await service.AssignAsync(
            Guid.NewGuid(),
            order.Id,
            new AssignOrderRequest { AgentId = Guid.NewGuid() });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(0, repository.SaveCount);
    }

    private static Order CreateOrder(OrderStatus status)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            Status = status
        };
    }
}
