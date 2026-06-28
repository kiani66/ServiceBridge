using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class AdminOrderQueryRepository : IAdminOrderQueryRepository
{
    private readonly ServiceBridgeDbContext _context;

    public AdminOrderQueryRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<AdminAssignableOrderDto>>
        GetPaidUnassignedOrdersAsync(int page, int pageSize)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(x =>
                x.Status == OrderStatus.Paid &&
                x.AssignedAgentId == null);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.PaidAt)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminAssignableOrderDto
            {
                OrderId = x.Id,
                OrderNumber = x.OrderNumber,
                ServiceName = x.Service.Name,
                PlanName = x.VariantNameSnapshot,
                PlanDurationDays = x.VariantDurationDaysSnapshot,
                PlanRegionCode = x.VariantRegionCodeSnapshot,
                CustomerName = x.User.FullName,
                FinalAmountIrr = x.FinalAmountIrr,
                PaidAt = x.PaidAt!.Value
            })
            .ToListAsync();

        return new PagedResultDto<AdminAssignableOrderDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyCollection<AgentSummaryDto>> GetActiveAgentsAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Where(x => x.Role == UserRole.Agent && x.IsActive)
            .OrderBy(x => x.FullName)
            .Select(x => new AgentSummaryDto
            {
                AgentId = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                ActiveOrderCount = _context.Orders.Count(order =>
                    order.AssignedAgentId == x.Id &&
                    (order.Status == OrderStatus.Assigned ||
                     order.Status == OrderStatus.Processing))
            })
            .ToListAsync();
    }

    public async Task<AdminOrderDashboardSummaryDto> GetDashboardSummaryAsync()
    {
        return new AdminOrderDashboardSummaryDto
        {
            PendingPaymentOrderCount = await _context.Orders.CountAsync(
                x => x.Status == OrderStatus.WaitingForPayment),
            PaidOrderCount = await _context.Orders.CountAsync(
                x => x.Status == OrderStatus.Paid),
            UnassignedOrderCount = await _context.Orders.CountAsync(
                x => x.Status == OrderStatus.Paid && x.AssignedAgentId == null)
        };
    }
}
