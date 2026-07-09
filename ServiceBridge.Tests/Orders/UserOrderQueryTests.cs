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
        var paidAt = new DateTime(2026, 7, 9, 8, 30, 0, DateTimeKind.Utc);
        var repository = new FakeUserOrderQueryRepository();
        repository.OrdersByUser[currentUserId] =
        [
            new UserOrderListItemDto
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "SB-1001",
                ServiceName = "ChatGPT Plus",
                Status = OrderStatus.WaitingForPayment.ToString(),
                PaymentStatus = PaymentStatus.Completed.ToString(),
                FinalPriceIrr = 12_500_000m,
                CreatedAt = paidAt.AddHours(-2),
                PaidAt = paidAt
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
        var item = result.Data.Items.Single();
        Assert.True(item.CanCancel);
        Assert.Equal("SB-1001", item.OrderNumber);
        Assert.Equal("ChatGPT Plus", item.ServiceName);
        Assert.Equal(OrderStatus.WaitingForPayment.ToString(), item.Status);
        Assert.Equal(PaymentStatus.Completed.ToString(), item.PaymentStatus);
        Assert.Equal(12_500_000m, item.FinalPriceIrr);
        Assert.Equal(paidAt, item.PaidAt);
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

    [Fact]
    public void UserOrderListItemDto_DoesNotExposeSensitiveCredentialFields()
    {
        var json = JsonSerializer.Serialize(new UserOrderListItemDto
        {
            OrderId = Guid.NewGuid(),
            OrderNumber = "SB-1002",
            ServiceName = "Streaming",
            Status = OrderStatus.Paid.ToString(),
            PaymentStatus = PaymentStatus.Completed.ToString(),
            FinalPriceIrr = 4_200_000m,
            CreatedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow
        });

        Assert.Contains(nameof(UserOrderListItemDto.OrderNumber), json);
        Assert.Contains(nameof(UserOrderListItemDto.ServiceName), json);
        Assert.Contains(nameof(UserOrderListItemDto.Status), json);
        Assert.Contains(nameof(UserOrderListItemDto.FinalPriceIrr), json);
        Assert.Contains(nameof(UserOrderListItemDto.PaidAt), json);
        Assert.DoesNotContain("Credential", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Encrypted", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMyOrderDetail_IncludesTimelineAndCustomerVisibleNotes()
    {
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var repository = new FakeUserOrderQueryRepository();
        var detail = CreateDetail(orderId);
        detail.Notes =
        [
            new UserVisibleOrderNoteDto
            {
                Text = "Your account is being prepared.",
                AuthorName = "Support Agent",
                CreatedAt = DateTime.UtcNow
            }
        ];
        detail.StatusHistory =
        [
            new OrderStatusHistoryDto
            {
                PreviousStatus = null,
                Status = OrderStatus.Created.ToString(),
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            },
            new OrderStatusHistoryDto
            {
                PreviousStatus = OrderStatus.Created.ToString(),
                Status = OrderStatus.WaitingForPayment.ToString(),
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            }
        ];
        repository.Details[(userId, orderId)] = detail;

        var query = new GetMyOrderDetailQuery(repository, new OrderStatusPolicy());
        var result = await query.ExecuteAsync(userId, orderId);

        Assert.True(result.Success);
        Assert.Equal(userId, repository.LastDetailUserId);
        Assert.Equal(orderId, repository.LastDetailOrderId);
        Assert.Single(result.Data!.Notes);
        Assert.Equal("Your account is being prepared.", result.Data.Notes.Single().Text);
        Assert.Equal(2, result.Data.StatusHistory.Count);
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
