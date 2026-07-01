using System.Text.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.AgentOrders;

public class AgentOrderQueryTests
{
    [Fact]
    public async Task List_ReturnsOnlyCurrentAgentsOrdersAndBoundsPaging()
    {
        var currentAgentId = Guid.NewGuid();
        var otherAgentId = Guid.NewGuid();
        var repository = new FakeAgentOrderQueryRepository();
        repository.OrdersByAgent[currentAgentId] =
        [
            new AgentOrderListItemDto { OrderId = Guid.NewGuid() }
        ];
        repository.OrdersByAgent[otherAgentId] =
        [
            new AgentOrderListItemDto { OrderId = Guid.NewGuid() }
        ];
        var service = new AgentOrderQueryService(repository);

        var result = await service.GetAssignedOrdersAsync(
            currentAgentId,
            0,
            1000,
            OrderStatus.Assigned,
            "  customer  ");

        Assert.True(result.Success);
        Assert.Single(result.Data!.Items);
        Assert.Equal(currentAgentId, repository.LastAgentId);
        Assert.Equal(1, repository.LastPage);
        Assert.Equal(
            IAgentOrderQueryService.MaximumPageSize,
            repository.LastPageSize);
        Assert.Equal(OrderStatus.Assigned, repository.LastStatus);
        Assert.Equal("customer", repository.LastSearch);
    }

    [Fact]
    public async Task DashboardSummary_UsesCurrentAgentScope()
    {
        var currentAgentId = Guid.NewGuid();
        var otherAgentId = Guid.NewGuid();
        var repository = new FakeAgentOrderQueryRepository();
        repository.OrdersByAgent[currentAgentId] =
        [
            new AgentOrderListItemDto { OrderId = Guid.NewGuid() }
        ];
        repository.OrdersByAgent[otherAgentId] =
        [
            new AgentOrderListItemDto { OrderId = Guid.NewGuid() }
        ];
        var service = new AgentOrderQueryService(repository);

        var result = await service.GetDashboardSummaryAsync(currentAgentId);

        Assert.True(result.Success);
        Assert.Equal(1, result.Data!.AssignedOrders);
        Assert.Equal(currentAgentId, repository.LastAgentId);
    }

    [Fact]
    public async Task Detail_ForAnotherAgentsOrder_ReturnsNotFound()
    {
        var ownerAgentId = Guid.NewGuid();
        var currentAgentId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var repository = new FakeAgentOrderQueryRepository();
        repository.Details[(ownerAgentId, orderId)] =
            new AgentOrderDetailDto { OrderId = orderId };
        var service = new AgentOrderQueryService(repository);

        var result = await service.GetAssignedOrderDetailAsync(
            currentAgentId,
            orderId);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public void DetailDto_DoesNotExposeProtectedStorageValues()
    {
        var detail = new AgentOrderDetailDto
        {
            Fields =
            [
                new AgentOrderFieldDto
                {
                    FieldKey = "password",
                    FieldLabel = "Password",
                    Value = "plain-value-for-assigned-agent",
                    IsSensitive = true
                }
            ]
        };

        var json = JsonSerializer.Serialize(detail);

        Assert.Contains("plain-value-for-assigned-agent", json);
        Assert.DoesNotContain("EncryptedValue", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ProtectedValue", json, StringComparison.OrdinalIgnoreCase);
    }
}
