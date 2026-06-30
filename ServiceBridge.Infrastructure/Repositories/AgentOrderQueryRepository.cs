using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;

namespace ServiceBridge.Infrastructure.Repositories;

public class AgentOrderQueryRepository : IAgentOrderQueryRepository
{
    private readonly ServiceBridgeDbContext _context;
    private readonly IOrderFieldProtector _fieldProtector;

    public AgentOrderQueryRepository(
        ServiceBridgeDbContext context,
        IOrderFieldProtector fieldProtector)
    {
        _context = context;
        _fieldProtector = fieldProtector;
    }

    public async Task<PagedResultDto<AgentOrderListItemDto>>
        GetAssignedOrdersAsync(
            Guid agentId,
            int page,
            int pageSize,
            OrderStatus? status)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(x => x.AssignedAgentId == agentId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Status == OrderStatus.Assigned ? 0 : 1)
            .ThenBy(x => x.AssignedAt)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AgentOrderListItemDto
            {
                OrderId = x.Id,
                OrderNumber = x.OrderNumber,
                ServiceName = x.Service.Name,
                ServiceVariantId = x.ServiceVariantId,
                PlanName = x.VariantNameSnapshot,
                PlanDurationDays = x.VariantDurationDaysSnapshot,
                PlanRegionCode = x.VariantRegionCodeSnapshot,
                CustomerName = x.User.FullName,
                Status = x.Status.ToString(),
                CreatedAt = x.CreatedAt,
                AssignedAt = x.AssignedAt,
                HasPendingCustomerAction = x.CustomerActionRequests.Any(
                    request => !request.IsResolved)
            })
            .ToListAsync();

        return new PagedResultDto<AgentOrderListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AgentOrderDetailDto?> GetAssignedOrderDetailAsync(
        Guid agentId,
        Guid orderId)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(x => x.Id == orderId && x.AssignedAgentId == agentId)
            .Select(x => new AgentOrderDetailDto
            {
                OrderId = x.Id,
                OrderNumber = x.OrderNumber,
                ServiceName = x.Service.Name,
                Provider = x.Service.Provider.Name,
                CustomerName = x.User.FullName,
                CustomerEmail = x.User.Email,
                Status = x.Status.ToString(),
                FulfillmentMode = x.FulfillmentMode.ToString(),
                ServiceVariantId = x.ServiceVariantId,
                PlanName = x.VariantNameSnapshot,
                PlanDurationDays = x.VariantDurationDaysSnapshot,
                PlanRegionCode = x.VariantRegionCodeSnapshot,
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

        if (order.Status is nameof(OrderStatus.Assigned) or
            nameof(OrderStatus.Processing))
        {
            var protectedFields = await _context.OrderFieldValues
                .AsNoTracking()
                .Where(x =>
                    x.OrderId == orderId &&
                    x.Actor == CredentialActor.Customer)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.FieldLabel)
                .Select(x => new ProtectedOrderField
                {
                    FieldKey = x.FieldKey,
                    FieldLabel = x.FieldLabel,
                    ProtectedValue = x.EncryptedValue,
                    IsSensitive = x.IsSensitive,
                    DisplayOrder = x.DisplayOrder
                })
                .ToListAsync();

            order.Fields = protectedFields
                .Select(x => new AgentOrderFieldDto
                {
                    FieldKey = x.FieldKey,
                    FieldLabel = x.FieldLabel,
                    Value = _fieldProtector.Unprotect(x.ProtectedValue),
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
                        AccessedByUserId = agentId,
                        AccessType = "AgentCredentialView",
                        CreatedAt = DateTime.UtcNow
                    });
                await _context.SaveChangesAsync();
            }

            var deliveryDefinitions = await _context.Orders
                .AsNoTracking()
                .Where(x => x.Id == orderId && x.AssignedAgentId == agentId)
                .SelectMany(x => x.Service.RequiredFields
                    .Where(field =>
                        field.FulfillmentMode == x.FulfillmentMode &&
                        field.Actor == CredentialActor.Agent))
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Label)
                .Select(x => new
                {
                    x.FieldKey,
                    x.Label,
                    FieldType = x.FieldType.ToString(),
                    x.IsRequired,
                    x.IsSensitive,
                    x.IsVisibleToCustomerAfterCompletion,
                    x.DisplayOrder
                })
                .ToListAsync();

            var protectedDeliveryFields = await _context.OrderFieldValues
                .AsNoTracking()
                .Where(x =>
                    x.OrderId == orderId &&
                    x.Actor == CredentialActor.Agent)
                .Select(x => new
                {
                    x.FieldKey,
                    x.EncryptedValue
                })
                .ToListAsync();

            var deliveryValues = protectedDeliveryFields.ToDictionary(
                x => x.FieldKey,
                StringComparer.OrdinalIgnoreCase);

            order.DeliveryFields = deliveryDefinitions
                .Select(x => new AgentOrderFieldDto
                {
                    FieldKey = x.FieldKey,
                    FieldLabel = x.Label,
                    Value = deliveryValues.TryGetValue(x.FieldKey, out var value)
                        ? _fieldProtector.Unprotect(value.EncryptedValue)
                        : string.Empty,
                    FieldType = x.FieldType,
                    IsRequired = x.IsRequired,
                    IsSensitive = x.IsSensitive,
                    IsVisibleToCustomerAfterCompletion =
                        x.IsVisibleToCustomerAfterCompletion,
                    DisplayOrder = x.DisplayOrder
                })
                .ToList();
        }

        order.Notes = await _context.OrderNotes
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new AgentOrderNoteDto
            {
                AuthorName = x.Author.FullName,
                Text = x.Text,
                IsVisibleToCustomer = x.IsVisibleToCustomer,
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

    private sealed class ProtectedOrderField
    {
        public string FieldKey { get; set; } = string.Empty;
        public string FieldLabel { get; set; } = string.Empty;
        public string ProtectedValue { get; set; } = string.Empty;
        public bool IsSensitive { get; set; }
        public int DisplayOrder { get; set; }
    }
}
