using System.Text.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Orders;
using ServiceBridge.Application.Services;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Orders;

public class UserOrderQueryTests
{
    [Fact]
    public async Task GetMyOrders_ReturnsOnlyRepositoryResultsForCurrentUser()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var repository = new FakeUserOrderQueryRepository();
        repository.OrdersByUser[currentUserId] =
        [
            new UserOrderListItemDto
            {
                OrderId = Guid.NewGuid(),
                Status = OrderStatus.WaitingForPayment.ToString()
            }
        ];
        repository.OrdersByUser[otherUserId] =
        [
            new UserOrderListItemDto
            {
                OrderId = Guid.NewGuid(),
                Status = OrderStatus.Completed.ToString()
            }
        ];

        var query = new GetMyOrdersQuery(repository, new OrderStatusPolicy());
        var result = await query.ExecuteAsync(
            currentUserId,
            1,
            20,
            OrderStatus.WaitingForPayment);

        Assert.True(result.Success);
        Assert.Equal(currentUserId, repository.LastListUserId);
        Assert.Single(result.Data!.Items);
        Assert.True(result.Data.Items.Single().CanCancel);
        Assert.Equal(1, repository.LastPage);
        Assert.Equal(20, repository.LastPageSize);
        Assert.Equal(OrderStatus.WaitingForPayment, repository.LastStatus);
    }

    [Fact]
    public async Task GetMyOrders_BoundsInvalidPagingValues()
    {
        var repository = new FakeUserOrderQueryRepository();
        var query = new GetMyOrdersQuery(repository, new OrderStatusPolicy());

        var result = await query.ExecuteAsync(Guid.NewGuid(), 0, 1000, null);

        Assert.True(result.Success);
        Assert.Equal(1, repository.LastPage);
        Assert.Equal(GetMyOrdersQuery.MaximumPageSize, repository.LastPageSize);
    }

    [Fact]
    public async Task GetMyOrderDetail_ForAnotherUsersOrder_ReturnsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var repository = new FakeUserOrderQueryRepository();
        repository.Details[(ownerId, orderId)] = CreateDetail(orderId);

        var query = new GetMyOrderDetailQuery(repository, new OrderStatusPolicy());
        var result = await query.ExecuteAsync(currentUserId, orderId);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task GetMyOrderDetail_WhenOrderIdIsInvalid_ReturnsNotFound()
    {
        var repository = new FakeUserOrderQueryRepository();
        var query = new GetMyOrderDetailQuery(repository, new OrderStatusPolicy());

        var result = await query.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public void UserOrderDetailDto_DoesNotSerializeEncryptedCredentials()
    {
        var json = JsonSerializer.Serialize(CreateDetail(Guid.NewGuid()));

        Assert.DoesNotContain("EncryptedValue", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FieldValues", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret-password", json, StringComparison.OrdinalIgnoreCase);
    }

    private static UserOrderDetailDto CreateDetail(Guid orderId)
    {
        return new UserOrderDetailDto
        {
            OrderId = orderId,
            OrderNumber = "PB-ORDER",
            ServiceName = "ChatGPT Plus",
            Provider = "OpenAI",
            Status = OrderStatus.WaitingForPayment.ToString()
        };
    }
}
