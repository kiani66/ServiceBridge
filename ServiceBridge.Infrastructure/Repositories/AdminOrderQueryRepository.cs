using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class AdminOrderQueryRepository : IAdminOrderQueryRepository
{
    private readonly ServiceBridgeDbContext _context;
    private readonly IOrderFieldProtector _fieldProtector;

    public AdminOrderQueryRepository(
        ServiceBridgeDbContext context,
        IOrderFieldProtector fieldProtector)
    {
        _context = context;
        _fieldProtector = fieldProtector;
    }

    public async Task<PagedResultDto<AdminOrderListItemDto>> GetOrdersAsync(
        int page,
        int pageSize,
        OrderStatus? status,
        string? search)
    {
        var query = _context.Orders.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.OrderNumber.Contains(term) ||
                x.User.FullName.Contains(term) ||
                x.User.Email.Contains(term) ||
                x.Service.Name.Contains(term) ||
                (x.VariantNameSnapshot != null &&
                    x.VariantNameSnapshot.Contains(term)) ||
                (x.AssignedAgent != null &&
                    x.AssignedAgent.FullName.Contains(term)));
        }

        var totalCount = await query.CountAsync();
        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                OrderId = x.Id,
                x.OrderNumber,
                CustomerName = x.User.FullName,
                CustomerEmail = x.User.Email,
                ServiceName = x.Service.Name,
                x.ServiceVariantId,
                PlanName = x.VariantNameSnapshot,
                PlanDurationDays = x.VariantDurationDaysSnapshot,
                PlanRegionCode = x.VariantRegionCodeSnapshot,
                OrderStatus = x.Status.ToString(),
                AssignedAgentName = x.AssignedAgent == null
                    ? null
                    : x.AssignedAgent.FullName,
                x.CreatedAt
            })
            .ToListAsync();

        var orderIds = rows.Select(x => x.OrderId).ToArray();
        var paymentStatuses = await _context.Payments
            .AsNoTracking()
            .Where(payment => orderIds.Contains(payment.OrderId))
            .OrderByDescending(payment => payment.CreatedAt)
            .ThenByDescending(payment => payment.Id)
            .Select(payment => new
            {
                payment.OrderId,
                payment.Status
            })
            .ToListAsync();
        var latestPaymentStatuses = paymentStatuses
            .GroupBy(payment => payment.OrderId)
            .ToDictionary(group => group.Key, group => group.First().Status);

        var items = rows
            .Select(x => new AdminOrderListItemDto
            {
                OrderId = x.OrderId,
                OrderNumber = x.OrderNumber,
                CustomerName = x.CustomerName,
                CustomerEmail = x.CustomerEmail,
                ServiceName = x.ServiceName,
                ServiceVariantId = x.ServiceVariantId,
                PlanName = x.PlanName,
                PlanDurationDays = x.PlanDurationDays,
                PlanRegionCode = x.PlanRegionCode,
                PaymentStatus = latestPaymentStatuses.TryGetValue(
                    x.OrderId,
                    out var paymentStatus)
                    ? paymentStatus.ToString()
                    : "Not started",
                OrderStatus = x.OrderStatus,
                AssignedAgentName = x.AssignedAgentName,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        return new PagedResultDto<AdminOrderListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
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
                ServiceVariantId = x.ServiceVariantId,
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
                     order.Status == OrderStatus.Processing)),
                AssignedOrderCount = _context.Orders.Count(order =>
                    order.AssignedAgentId == x.Id)
            })
            .ToListAsync();
    }

    public async Task<AdminOrderDashboardSummaryDto> GetDashboardSummaryAsync()
    {
        return new AdminOrderDashboardSummaryDto
        {
            TotalOrderCount = await _context.Orders.CountAsync(),
            PendingPaymentOrderCount = await _context.Orders.CountAsync(
                x => x.Status == OrderStatus.WaitingForPayment),
            PaidOrderCount = await _context.Orders.CountAsync(
                x => x.Status == OrderStatus.Paid),
            ProcessingOrderCount = await _context.Orders.CountAsync(
                x => x.Status == OrderStatus.Processing),
            CompletedOrderCount = await _context.Orders.CountAsync(
                x => x.Status == OrderStatus.Completed),
            FailedOrderCount = await _context.Orders.CountAsync(
                x => x.Status == OrderStatus.Failed),
            UnassignedOrderCount = await _context.Orders.CountAsync(
                x => x.Status == OrderStatus.Paid && x.AssignedAgentId == null)
        };
    }

    public async Task<AdminOrderDetailDto?> GetOrderDetailAsync(
        Guid adminId,
        Guid orderId)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(x => x.Id == orderId)
            .Select(x => new AdminOrderDetailDto
            {
                OrderId = x.Id,
                OrderNumber = x.OrderNumber,
                CustomerName = x.User.FullName,
                CustomerEmail = x.User.Email,
                ServiceName = x.Service.Name,
                Provider = x.Service.Provider.Name,
                FulfillmentMode = x.FulfillmentMode.ToString(),
                ServiceVariantId = x.ServiceVariantId,
                PlanName = x.VariantNameSnapshot,
                PlanDurationDays = x.VariantDurationDaysSnapshot,
                PlanRegionCode = x.VariantRegionCodeSnapshot,
                Status = x.Status.ToString(),
                AssignedAgentName = x.AssignedAgent == null
                    ? null
                    : x.AssignedAgent.FullName,
                BasePriceUsdSnapshot = x.ForeignPrice,
                ExchangeRateSnapshot = x.ExchangeRateSnapshot,
                MarginSnapshot = x.MarginPercentSnapshot,
                FinalPriceIrr = x.FinalAmountIrr,
                CreatedAt = x.CreatedAt,
                PaidAt = x.PaidAt,
                AssignedAt = x.AssignedAt,
                CompletedAt = x.CompletedAt
            })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return null;
        }

        var protectedFields = await _context.OrderFieldValues
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.Actor)
            .ThenBy(x => x.DisplayOrder)
            .ThenBy(x => x.FieldLabel)
            .Select(x => new
            {
                x.FieldKey,
                x.FieldLabel,
                ProtectedValue = x.EncryptedValue,
                FieldType = x.Actor.ToString(),
                x.IsSensitive,
                x.DisplayOrder
            })
            .ToListAsync();

        order.Credentials = protectedFields
            .Select(x => new AgentOrderFieldDto
            {
                FieldKey = x.FieldKey,
                FieldLabel = x.FieldLabel,
                Value = _fieldProtector.Unprotect(x.ProtectedValue),
                FieldType = x.FieldType,
                IsSensitive = x.IsSensitive,
                DisplayOrder = x.DisplayOrder
            })
            .ToList();

        if (protectedFields.Count > 0)
        {
            await _context.OrderCredentialAccessLogs.AddAsync(
                new OrderCredentialAccessLog
                {
                    OrderId = orderId,
                    AccessedByUserId = adminId,
                    AccessType = "AdminCredentialView",
                    CreatedAt = DateTime.UtcNow
                });
            await _context.SaveChangesAsync();
        }

        order.Payments = await _context.Payments
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new PaymentHistoryDto
            {
                PaymentId = x.Id,
                AmountIrr = x.AmountIRR,
                Status = x.Status.ToString(),
                GatewayReference = x.GatewayReference,
                CreatedAt = x.CreatedAt,
                CompletedAt = x.ConfirmedAt
            })
            .ToListAsync();

        order.StatusHistory = await _context.OrderStatusHistory
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new OrderStatusHistoryDto
            {
                PreviousStatus = x.PreviousStatus.HasValue
                    ? x.PreviousStatus.Value.ToString()
                    : null,
                Status = x.NewStatus.ToString(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        order.Notes = await _context.OrderNotes
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AgentOrderNoteDto
            {
                AuthorName = x.Author.FullName,
                Text = x.Text,
                IsVisibleToCustomer = x.IsVisibleToCustomer,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        order.Assignments = await _context.AgentAssignments
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.AssignedAt)
            .Select(x => new AdminOrderAssignmentHistoryDto
            {
                AgentName = x.Agent.FullName,
                AssignedByAdminName = x.AssignedByAdmin == null
                    ? null
                    : x.AssignedByAdmin.FullName,
                AssignedAt = x.AssignedAt,
                Notes = x.Notes
            })
            .ToListAsync();

        return order;
    }

    public async Task<PagedResultDto<AdminPaymentListItemDto>> GetPaymentsAsync(
        int page,
        int pageSize,
        PaymentStatus? status,
        string? search)
    {
        var query = _context.Payments.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Order.OrderNumber.Contains(term) ||
                x.Order.User.FullName.Contains(term) ||
                x.Order.User.Email.Contains(term) ||
                (x.GatewayReference != null && x.GatewayReference.Contains(term)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminPaymentListItemDto
            {
                PaymentId = x.Id,
                OrderId = x.OrderId,
                OrderNumber = x.Order.OrderNumber,
                CustomerName = x.Order.User.FullName,
                Status = x.Status.ToString(),
                AmountIrr = x.AmountIRR,
                TransactionNumber = x.GatewayReference,
                PaymentDate = x.ConfirmedAt ?? x.FailedAt ?? x.RefundedAt ?? x.CreatedAt
            })
            .ToListAsync();

        return new PagedResultDto<AdminPaymentListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
