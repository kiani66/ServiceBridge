using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Tests.TestDoubles;

internal class FakeAgentOrderQueryRepository : IAgentOrderQueryRepository
{
    public Dictionary<Guid, IReadOnlyCollection<AgentOrderListItemDto>> OrdersByAgent
        { get; } = new();
    public Dictionary<(Guid AgentId, Guid OrderId), AgentOrderDetailDto> Details
        { get; } = new();
    public Guid? LastAgentId { get; private set; }
    public int? LastPage { get; private set; }
    public int? LastPageSize { get; private set; }
    public OrderStatus? LastStatus { get; private set; }

    public Task<PagedResultDto<AgentOrderListItemDto>> GetAssignedOrdersAsync(
        Guid agentId,
        int page,
        int pageSize,
        OrderStatus? status)
    {
        LastAgentId = agentId;
        LastPage = page;
        LastPageSize = pageSize;
        LastStatus = status;

        var orders = OrdersByAgent.TryGetValue(agentId, out var assignedOrders)
            ? assignedOrders
            : Array.Empty<AgentOrderListItemDto>();

        return Task.FromResult(new PagedResultDto<AgentOrderListItemDto>
        {
            Items = orders,
            Page = page,
            PageSize = pageSize,
            TotalCount = orders.Count
        });
    }

    public Task<AgentOrderDetailDto?> GetAssignedOrderDetailAsync(
        Guid agentId,
        Guid orderId)
    {
        Details.TryGetValue((agentId, orderId), out var detail);
        return Task.FromResult(detail);
    }
}
