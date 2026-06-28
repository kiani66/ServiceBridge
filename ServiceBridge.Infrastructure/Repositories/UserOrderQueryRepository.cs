using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class UserOrderQueryRepository : IUserOrderQueryRepository
{
    private readonly ServiceBridgeDbContext _context;
    private readonly IOrderFieldProtector _fieldProtector;

    public UserOrderQueryRepository(
        ServiceBridgeDbContext context,
        IOrderFieldProtector fieldProtector)
    {
        _context = context;
        _fieldProtector = fieldProtector;
    }

    public async Task<PagedResultDto<UserOrderListItemDto>> GetMyOrdersAsync(
        Guid userId,
        int page,
        int pageSize,
        OrderStatus? status)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserOrderListItemDto
            {
                OrderId = x.Id,
                OrderNumber = x.OrderNumber,
                ServiceName = x.Service.Name,
                Provider = x.Service.Provider,
                FulfillmentMode = x.FulfillmentMode.ToString(),
                PlanName = x.PlanNameSnapshot,
                PlanDurationDays = x.PlanDurationDaysSnapshot,
                PlanRegionCode = x.PlanRegionCodeSnapshot,
                BasePriceUsdSnapshot = x.ForeignPrice,
                FinalPriceIrr = x.FinalAmountIrr,
                Status = x.Status.ToString(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return new PagedResultDto<UserOrderListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<UserOrderDetailDto?> GetMyOrderDetailAsync(
        Guid userId,
        Guid orderId)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(x => x.Id == orderId && x.UserId == userId)
            .Select(x => new UserOrderDetailDto
            {
                OrderId = x.Id,
                OrderNumber = x.OrderNumber,
                ServiceName = x.Service.Name,
                Provider = x.Service.Provider,
                FulfillmentMode = x.FulfillmentMode.ToString(),
                PlanName = x.PlanNameSnapshot,
                PlanDurationDays = x.PlanDurationDaysSnapshot,
                PlanRegionCode = x.PlanRegionCodeSnapshot,
                BasePriceUsdSnapshot = x.ForeignPrice,
                ExchangeRateSnapshot = x.ExchangeRateSnapshot,
                MarginSnapshot = x.MarginPercentSnapshot,
                FinalPriceIrr = x.FinalAmountIrr,
                Status = x.Status.ToString(),
                CreatedAt = x.CreatedAt,
                PaidAt = x.PaidAt,
                AssignedAt = x.AssignedAt,
                CompletedAt = x.CompletedAt,
                CancelledAt = x.CancelledAt,
                CanCancel = x.Status == OrderStatus.Created ||
                    x.Status == OrderStatus.WaitingForPayment ||
                    x.Status == OrderStatus.Paid ||
                    x.Status == OrderStatus.Assigned
            })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return null;
        }

        var protectedFields = await _context.OrderFieldValues
            .AsNoTracking()
            .Where(x =>
                x.OrderId == orderId &&
                x.Actor == CredentialActor.Customer)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.FieldLabel)
            .Select(x => new
            {
                x.FieldLabel,
                x.EncryptedValue,
                x.IsSensitive,
                x.DisplayOrder
            })
            .ToListAsync();

        order.Fields = protectedFields
            .Select(x => new UserOrderFieldDto
            {
                FieldLabel = x.FieldLabel,
                Value = x.IsSensitive
                    ? "••••••••"
                    : _fieldProtector.Unprotect(x.EncryptedValue),
                IsSensitive = x.IsSensitive,
                DisplayOrder = x.DisplayOrder
            })
            .ToList();

        if (order.Status == nameof(OrderStatus.Completed))
        {
            var protectedDeliveryFields = await _context.OrderFieldValues
                .AsNoTracking()
                .Where(x =>
                    x.OrderId == orderId &&
                    x.Actor == CredentialActor.Agent &&
                    x.IsVisibleToCustomerAfterCompletion)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.FieldLabel)
                .Select(x => new
                {
                    x.FieldLabel,
                    x.EncryptedValue,
                    x.IsSensitive,
                    x.DisplayOrder
                })
                .ToListAsync();

            order.DeliveryFields = protectedDeliveryFields
                .Select(x => new UserOrderFieldDto
                {
                    FieldLabel = x.FieldLabel,
                    Value = _fieldProtector.Unprotect(x.EncryptedValue),
                    IsSensitive = x.IsSensitive,
                    DisplayOrder = x.DisplayOrder
                })
                .ToList();

            if (protectedDeliveryFields.Count > 0)
            {
                await _context.OrderCredentialAccessLogs.AddAsync(
                    new ServiceBridge.Domain.Entities.OrderCredentialAccessLog
                    {
                        OrderId = orderId,
                        AccessedByUserId = userId,
                        AccessType = "CustomerDeliveryCredentialView",
                        CreatedAt = DateTime.UtcNow
                    });
                await _context.SaveChangesAsync();
            }
        }

        order.Notes = await _context.OrderNotes
            .AsNoTracking()
            .Where(x => x.OrderId == orderId && x.IsVisibleToCustomer)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new UserVisibleOrderNoteDto
            {
                Text = x.Text,
                AuthorName = x.Author.FullName,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        order.CustomerActions = await _context.CustomerActionRequests
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.RequestedAt)
            .Select(x => new CustomerActionRequestDto
            {
                Id = x.Id,
                RequestText = x.RequestText,
                CustomerResponse = x.CustomerResponse,
                RequestedAt = x.RequestedAt,
                RespondedAt = x.RespondedAt,
                IsResolved = x.IsResolved
            })
            .ToListAsync();

        order.Payments = await _context.Payments
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAt)
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

        return order;
    }
}
