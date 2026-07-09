using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Orders;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Orders;

public class RespondToCustomerActionCommandTests
{
    [Fact]
    public async Task ExecuteAsync_ForOwnOrderRequest_SavesResponseAndNotifiesAgent()
    {
        var userId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var order = CreateProcessingOrder(userId, agentId);
        order.RequestCustomerAction(agentId, "Please confirm your account email.");
        var action = Assert.Single(order.CustomerActionRequests);
        var repository = new FakeUserOrderCommandRepository { Order = order };
        var notifications = new FakeNotificationService();
        var realtimePublisher = new FakeRealtimeEventPublisher();
        var command = new RespondToCustomerActionCommand(
            repository,
            notifications,
            realtimePublisher);

        var result = await command.ExecuteAsync(
            userId,
            order.Id,
            new RespondToCustomerActionRequest
            {
                RequestId = action.Id,
                Response = "customer@example.com"
            });

        Assert.True(result.Success);
        Assert.True(action.IsResolved);
        Assert.Equal("customer@example.com", action.CustomerResponse);
        Assert.NotNull(action.RespondedAt);
        Assert.Equal(1, repository.SaveCount);
        Assert.Contains(
            nameof(FakeNotificationService.QueueCustomerActionRespondedAsync),
            notifications.QueuedTypes);
        Assert.Contains(
            nameof(FakeNotificationService.PublishQueuedRealtimeAsync),
            notifications.QueuedTypes);
        var realtimeEvent = Assert.Single(realtimePublisher.CustomerActionChanges);
        Assert.Equal(order.Id, realtimeEvent.OrderId);
        Assert.Equal(action.Id, realtimeEvent.RequestId);
        Assert.Equal(order.UserId, realtimeEvent.UserId);
        Assert.Equal(agentId, realtimeEvent.AgentId);
    }

    [Fact]
    public async Task ExecuteAsync_ForAnotherUsersOrder_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var order = CreateProcessingOrder(userId, agentId);
        order.RequestCustomerAction(agentId, "Please confirm your account email.");
        var action = Assert.Single(order.CustomerActionRequests);
        var repository = new FakeUserOrderCommandRepository { Order = order };
        var notifications = new FakeNotificationService();
        var command = new RespondToCustomerActionCommand(
            repository,
            notifications,
            new FakeRealtimeEventPublisher());

        var result = await command.ExecuteAsync(
            Guid.NewGuid(),
            order.Id,
            new RespondToCustomerActionRequest
            {
                RequestId = action.Id,
                Response = "customer@example.com"
            });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.False(action.IsResolved);
        Assert.Equal(0, repository.SaveCount);
        Assert.Empty(notifications.QueuedTypes);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestAlreadyResponded_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var order = CreateProcessingOrder(userId, agentId);
        order.RequestCustomerAction(agentId, "Please confirm your account email.");
        var action = Assert.Single(order.CustomerActionRequests);
        var repository = new FakeUserOrderCommandRepository { Order = order };
        var notifications = new FakeNotificationService();
        var command = new RespondToCustomerActionCommand(
            repository,
            notifications,
            new FakeRealtimeEventPublisher());

        var firstResult = await command.ExecuteAsync(
            userId,
            order.Id,
            new RespondToCustomerActionRequest
            {
                RequestId = action.Id,
                Response = "customer@example.com"
            });
        var secondResult = await command.ExecuteAsync(
            userId,
            order.Id,
            new RespondToCustomerActionRequest
            {
                RequestId = action.Id,
                Response = "second response"
            });

        Assert.True(firstResult.Success);
        Assert.False(secondResult.Success);
        Assert.Equal(409, secondResult.StatusCode);
        Assert.Equal("customer@example.com", action.CustomerResponse);
        Assert.Equal(1, repository.SaveCount);
        Assert.Equal(
            1,
            notifications.QueuedTypes.Count(
                type => type == nameof(FakeNotificationService.QueueCustomerActionRespondedAsync)));
    }

    [Fact]
    public async Task ExecuteAsync_WhenResponseIsEmpty_ReturnsValidationFailure()
    {
        var userId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var order = CreateProcessingOrder(userId, agentId);
        order.RequestCustomerAction(agentId, "Please confirm your account email.");
        var action = Assert.Single(order.CustomerActionRequests);
        var repository = new FakeUserOrderCommandRepository { Order = order };
        var notifications = new FakeNotificationService();
        var realtimePublisher = new FakeRealtimeEventPublisher();
        var command = new RespondToCustomerActionCommand(
            repository,
            notifications,
            realtimePublisher);

        var result = await command.ExecuteAsync(
            userId,
            order.Id,
            new RespondToCustomerActionRequest
            {
                RequestId = action.Id,
                Response = "   "
            });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.NotNull(result.ValidationErrors);
        Assert.Contains(nameof(RespondToCustomerActionRequest.Response), result.ValidationErrors.Keys);
        Assert.False(action.IsResolved);
        Assert.Equal(0, repository.SaveCount);
        Assert.Empty(notifications.QueuedTypes);
        Assert.Empty(realtimePublisher.CustomerActionChanges);
    }

    private static Order CreateProcessingOrder(Guid userId, Guid agentId)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "SB-1001",
            UserId = userId,
            AssignedAgentId = agentId,
            Status = OrderStatus.Processing
        };
    }
}
