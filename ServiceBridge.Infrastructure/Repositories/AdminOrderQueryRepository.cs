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
        var countsByStatus = await _context.Orders
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        var unassignedOrderCount = await _context.Orders
            .AsNoTracking()
            .CountAsync(x =>
                x.Status == OrderStatus.Paid &&
                x.AssignedAgentId == null);

        return new AdminOrderDashboardSummaryDto
        {
            TotalOrderCount = countsByStatus.Values.Sum(),
            PendingPaymentOrderCount = GetCount(OrderStatus.WaitingForPayment),
            PaidOrderCount = GetCount(OrderStatus.Paid),
            ProcessingOrderCount = GetCount(OrderStatus.Processing),
            CompletedOrderCount = GetCount(OrderStatus.Completed),
            FailedOrderCount = GetCount(OrderStatus.Failed),
            UnassignedOrderCount = unassignedOrderCount
        };

        int GetCount(OrderStatus status)
        {
            return countsByStatus.TryGetValue(status, out var count)
                ? count
                : 0;
        }
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

    public async Task<PagedResultDto<AdminAuditEventDto>> GetAuditEventsAsync(
        AdminAuditQueryDto query)
    {
        var events = await BuildAuditEventsAsync(query);
        events = ApplyTextFilters(events, query)
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.EventId)
            .ToList();

        var totalCount = events.Count;
        var items = events
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ToAuditDto)
            .ToList();

        return new PagedResultDto<AdminAuditEventDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyCollection<AdminOrderTimelineEventDto>?>
        GetOrderTimelineAsync(Guid orderId)
    {
        var exists = await _context.Orders
            .AsNoTracking()
            .AnyAsync(x => x.Id == orderId);
        if (!exists)
        {
            return null;
        }

        var events = await BuildAuditEventsAsync(new AdminAuditQueryDto
        {
            OrderId = orderId
        });

        return events
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.EventId)
            .Select(ToTimelineDto)
            .ToList();
    }

    private async Task<List<AuditEventProjection>> BuildAuditEventsAsync(
        AdminAuditQueryDto query)
    {
        var events = new List<AuditEventProjection>();

        if (ShouldLoadEventType(query.EventType, "Status"))
        {
            var statusQuery = _context.OrderStatusHistory.AsNoTracking();
            if (query.OrderId.HasValue)
            {
                statusQuery = statusQuery.Where(x => x.OrderId == query.OrderId.Value);
            }

            if (query.FromUtc.HasValue)
            {
                statusQuery = statusQuery.Where(x => x.CreatedAt >= query.FromUtc.Value);
            }

            if (query.ToUtc.HasValue)
            {
                statusQuery = statusQuery.Where(x => x.CreatedAt <= query.ToUtc.Value);
            }

            if (query.Status.HasValue)
            {
                statusQuery = statusQuery.Where(x => x.NewStatus == query.Status.Value);
            }

            var rows = await statusQuery
                .Select(x => new
                {
                    EventId = x.Id,
                    x.OrderId,
                    x.Order.OrderNumber,
                    CustomerName = x.Order.User.FullName,
                    CustomerEmail = x.Order.User.Email,
                    AssignedAgentName = x.Order.AssignedAgent == null
                        ? null
                        : x.Order.AssignedAgent.FullName,
                    x.CreatedAt,
                    x.PreviousStatus,
                    x.NewStatus,
                    x.ActorType,
                    ActorName = x.ChangedByUser == null
                        ? null
                        : x.ChangedByUser.FullName,
                    ActorEmail = x.ChangedByUser == null
                        ? null
                        : x.ChangedByUser.Email,
                    x.Note
                })
                .ToListAsync();

            events.AddRange(rows.Select(x => new AuditEventProjection
            {
                EventId = x.EventId,
                OrderId = x.OrderId,
                OrderNumber = x.OrderNumber,
                CustomerName = SafeName(x.CustomerName, x.CustomerEmail),
                CustomerEmail = x.CustomerEmail,
                AssignedAgentName = x.AssignedAgentName,
                OccurredAt = x.CreatedAt,
                EventType = "Status",
                Title = x.PreviousStatus.HasValue
                    ? $"Status changed to {FormatStatus(x.NewStatus.ToString())}"
                    : "Order created",
                Description = string.IsNullOrWhiteSpace(x.Note)
                    ? x.PreviousStatus.HasValue
                        ? $"{FormatStatus(x.PreviousStatus.Value.ToString())} changed to {FormatStatus(x.NewStatus.ToString())}."
                        : "Order timeline started."
                    : x.Note,
                Status = x.NewStatus.ToString(),
                ActorType = x.ActorType.ToString(),
                ActorName = ActorName(x.ActorName, x.ActorEmail, x.ActorType.ToString()),
                ActorEmail = x.ActorEmail,
                Tone = OrderTone(x.NewStatus.ToString())
            }));
        }

        if (ShouldLoadEventType(query.EventType, "Payment"))
        {
            var paymentQuery = _context.PaymentEvents.AsNoTracking();
            if (query.OrderId.HasValue)
            {
                paymentQuery = paymentQuery.Where(
                    x => x.Payment.OrderId == query.OrderId.Value);
            }

            if (query.FromUtc.HasValue)
            {
                paymentQuery = paymentQuery.Where(
                    x => x.CreatedAt >= query.FromUtc.Value);
            }

            if (query.ToUtc.HasValue)
            {
                paymentQuery = paymentQuery.Where(
                    x => x.CreatedAt <= query.ToUtc.Value);
            }

            if (query.Status.HasValue)
            {
                paymentQuery = paymentQuery.Where(
                    x => x.Payment.Order.Status == query.Status.Value);
            }

            var rows = await paymentQuery
                .Select(x => new
                {
                    EventId = x.Id,
                    x.Payment.OrderId,
                    x.Payment.Order.OrderNumber,
                    CustomerName = x.Payment.Order.User.FullName,
                    CustomerEmail = x.Payment.Order.User.Email,
                    AssignedAgentName = x.Payment.Order.AssignedAgent == null
                        ? null
                        : x.Payment.Order.AssignedAgent.FullName,
                    x.CreatedAt,
                    x.EventType,
                    x.GatewayReference,
                    x.Note
                })
                .ToListAsync();

            events.AddRange(rows.Select(x => new AuditEventProjection
            {
                EventId = x.EventId,
                OrderId = x.OrderId,
                OrderNumber = x.OrderNumber,
                CustomerName = SafeName(x.CustomerName, x.CustomerEmail),
                CustomerEmail = x.CustomerEmail,
                AssignedAgentName = x.AssignedAgentName,
                OccurredAt = x.CreatedAt,
                EventType = "Payment",
                Title = $"Payment {FormatStatus(x.EventType.ToString())}",
                Description = PaymentDescription(x.GatewayReference, x.Note),
                Status = x.EventType.ToString(),
                ActorType = OrderLogActorType.System.ToString(),
                ActorName = "System",
                Tone = PaymentTone(x.EventType.ToString())
            }));
        }

        if (ShouldLoadEventType(query.EventType, "Refund"))
        {
            var refundQuery = _context.RefundRequestStatusHistory.AsNoTracking();
            if (query.OrderId.HasValue)
            {
                refundQuery = refundQuery.Where(
                    x => x.RefundRequest.OrderId == query.OrderId.Value);
            }

            if (query.FromUtc.HasValue)
            {
                refundQuery = refundQuery.Where(
                    x => x.CreatedAt >= query.FromUtc.Value);
            }

            if (query.ToUtc.HasValue)
            {
                refundQuery = refundQuery.Where(
                    x => x.CreatedAt <= query.ToUtc.Value);
            }

            if (query.Status.HasValue)
            {
                refundQuery = refundQuery.Where(
                    x => x.RefundRequest.Order.Status == query.Status.Value);
            }

            var rows = await refundQuery
                .Select(x => new
                {
                    EventId = x.Id,
                    x.RefundRequest.OrderId,
                    x.RefundRequest.Order.OrderNumber,
                    CustomerName = x.RefundRequest.Order.User.FullName,
                    CustomerEmail = x.RefundRequest.Order.User.Email,
                    AssignedAgentName = x.RefundRequest.Order.AssignedAgent == null
                        ? null
                        : x.RefundRequest.Order.AssignedAgent.FullName,
                    x.CreatedAt,
                    x.PreviousStatus,
                    x.NewStatus,
                    x.ActorType,
                    x.ActorId,
                    x.Note
                })
                .ToListAsync();

            events.AddRange(rows.Select(x => new AuditEventProjection
            {
                EventId = x.EventId,
                OrderId = x.OrderId,
                OrderNumber = x.OrderNumber,
                CustomerName = SafeName(x.CustomerName, x.CustomerEmail),
                CustomerEmail = x.CustomerEmail,
                AssignedAgentName = x.AssignedAgentName,
                OccurredAt = x.CreatedAt,
                EventType = "Refund",
                Title = x.PreviousStatus.HasValue
                    ? $"Refund {FormatStatus(x.NewStatus.ToString())}"
                    : "Refund requested",
                Description = string.IsNullOrWhiteSpace(x.Note)
                    ? x.PreviousStatus.HasValue
                        ? $"{FormatStatus(x.PreviousStatus.Value.ToString())} changed to {FormatStatus(x.NewStatus.ToString())}."
                        : "Refund request created."
                    : x.Note,
                Status = x.NewStatus.ToString(),
                ActorType = x.ActorType.ToString(),
                ActorName = x.ActorType == OrderLogActorType.User
                    ? SafeName(x.CustomerName, x.CustomerEmail)
                    : x.ActorType.ToString(),
                Tone = RefundTone(x.NewStatus.ToString())
            }));
        }

        if (ShouldLoadEventType(query.EventType, "Note"))
        {
            var noteQuery = _context.OrderNotes.AsNoTracking();
            if (query.OrderId.HasValue)
            {
                noteQuery = noteQuery.Where(x => x.OrderId == query.OrderId.Value);
            }

            if (query.FromUtc.HasValue)
            {
                noteQuery = noteQuery.Where(x => x.CreatedAt >= query.FromUtc.Value);
            }

            if (query.ToUtc.HasValue)
            {
                noteQuery = noteQuery.Where(x => x.CreatedAt <= query.ToUtc.Value);
            }

            if (query.Status.HasValue)
            {
                noteQuery = noteQuery.Where(x => x.Order.Status == query.Status.Value);
            }

            var rows = await noteQuery
                .Select(x => new
                {
                    EventId = x.Id,
                    x.OrderId,
                    x.Order.OrderNumber,
                    CustomerName = x.Order.User.FullName,
                    CustomerEmail = x.Order.User.Email,
                    AssignedAgentName = x.Order.AssignedAgent == null
                        ? null
                        : x.Order.AssignedAgent.FullName,
                    x.CreatedAt,
                    x.Text,
                    x.IsVisibleToCustomer,
                    AuthorName = x.Author.FullName,
                    AuthorEmail = x.Author.Email,
                    AuthorRole = x.Author.Role
                })
                .ToListAsync();

            events.AddRange(rows.Select(x => new AuditEventProjection
            {
                EventId = x.EventId,
                OrderId = x.OrderId,
                OrderNumber = x.OrderNumber,
                CustomerName = SafeName(x.CustomerName, x.CustomerEmail),
                CustomerEmail = x.CustomerEmail,
                AssignedAgentName = x.AssignedAgentName,
                OccurredAt = x.CreatedAt,
                EventType = "Note",
                Title = x.IsVisibleToCustomer
                    ? "Customer-visible note added"
                    : "Internal note added",
                Description = x.Text,
                ActorType = x.AuthorRole.ToString(),
                ActorName = ActorName(
                    x.AuthorName,
                    x.AuthorEmail,
                    x.AuthorRole.ToString()),
                ActorEmail = x.AuthorEmail,
                Tone = x.IsVisibleToCustomer ? "info" : "neutral"
            }));
        }

        if (ShouldLoadEventType(query.EventType, "CredentialAccess"))
        {
            var credentialQuery = _context.OrderCredentialAccessLogs.AsNoTracking();
            if (query.OrderId.HasValue)
            {
                credentialQuery = credentialQuery.Where(
                    x => x.OrderId == query.OrderId.Value);
            }

            if (query.FromUtc.HasValue)
            {
                credentialQuery = credentialQuery.Where(
                    x => x.CreatedAt >= query.FromUtc.Value);
            }

            if (query.ToUtc.HasValue)
            {
                credentialQuery = credentialQuery.Where(
                    x => x.CreatedAt <= query.ToUtc.Value);
            }

            if (query.Status.HasValue)
            {
                credentialQuery = credentialQuery.Where(
                    x => x.Order.Status == query.Status.Value);
            }

            var rows = await credentialQuery
                .Select(x => new
                {
                    EventId = x.Id,
                    x.OrderId,
                    x.Order.OrderNumber,
                    CustomerName = x.Order.User.FullName,
                    CustomerEmail = x.Order.User.Email,
                    AssignedAgentName = x.Order.AssignedAgent == null
                        ? null
                        : x.Order.AssignedAgent.FullName,
                    x.CreatedAt,
                    x.AccessType,
                    ActorName = x.AccessedByUser.FullName,
                    ActorEmail = x.AccessedByUser.Email,
                    ActorRole = x.AccessedByUser.Role
                })
                .ToListAsync();

            events.AddRange(rows.Select(x => new AuditEventProjection
            {
                EventId = x.EventId,
                OrderId = x.OrderId,
                OrderNumber = x.OrderNumber,
                CustomerName = SafeName(x.CustomerName, x.CustomerEmail),
                CustomerEmail = x.CustomerEmail,
                AssignedAgentName = x.AssignedAgentName,
                OccurredAt = x.CreatedAt,
                EventType = "CredentialAccess",
                Title = "Credential access recorded",
                Description = $"{FormatStatus(x.AccessType)}. Credential values are not included in audit results.",
                ActorType = x.ActorRole.ToString(),
                ActorName = ActorName(
                    x.ActorName,
                    x.ActorEmail,
                    x.ActorRole.ToString()),
                ActorEmail = x.ActorEmail,
                Tone = "warning"
            }));
        }

        if (ShouldLoadEventType(query.EventType, "Assignment"))
        {
            var assignmentQuery = _context.AgentAssignments.AsNoTracking();
            if (query.OrderId.HasValue)
            {
                assignmentQuery = assignmentQuery.Where(
                    x => x.OrderId == query.OrderId.Value);
            }

            if (query.FromUtc.HasValue)
            {
                assignmentQuery = assignmentQuery.Where(
                    x => x.AssignedAt >= query.FromUtc.Value);
            }

            if (query.ToUtc.HasValue)
            {
                assignmentQuery = assignmentQuery.Where(
                    x => x.AssignedAt <= query.ToUtc.Value);
            }

            if (query.Status.HasValue)
            {
                assignmentQuery = assignmentQuery.Where(
                    x => x.Order.Status == query.Status.Value);
            }

            var rows = await assignmentQuery
                .Select(x => new
                {
                    EventId = x.Id,
                    x.OrderId,
                    x.Order.OrderNumber,
                    CustomerName = x.Order.User.FullName,
                    CustomerEmail = x.Order.User.Email,
                    AssignedAgentName = x.Agent.FullName,
                    OccurredAt = x.AssignedAt,
                    x.Notes,
                    AdminName = x.AssignedByAdmin == null
                        ? null
                        : x.AssignedByAdmin.FullName,
                    AdminEmail = x.AssignedByAdmin == null
                        ? null
                        : x.AssignedByAdmin.Email
                })
                .ToListAsync();

            events.AddRange(rows.Select(x => new AuditEventProjection
            {
                EventId = x.EventId,
                OrderId = x.OrderId,
                OrderNumber = x.OrderNumber,
                CustomerName = SafeName(x.CustomerName, x.CustomerEmail),
                CustomerEmail = x.CustomerEmail,
                AssignedAgentName = x.AssignedAgentName,
                OccurredAt = x.OccurredAt,
                EventType = "Assignment",
                Title = "Agent assigned",
                Description = string.IsNullOrWhiteSpace(x.Notes)
                    ? $"Assigned to {x.AssignedAgentName}."
                    : $"Assigned to {x.AssignedAgentName}. {x.Notes}",
                ActorType = OrderLogActorType.Admin.ToString(),
                ActorName = ActorName(
                    x.AdminName,
                    x.AdminEmail,
                    OrderLogActorType.Admin.ToString()),
                ActorEmail = x.AdminEmail,
                Tone = "info"
            }));
        }

        if (ShouldLoadEventType(query.EventType, "CustomerAction"))
        {
            await AddCustomerActionEventsAsync(events, query);
        }

        return events;
    }

    private async Task AddCustomerActionEventsAsync(
        List<AuditEventProjection> events,
        AdminAuditQueryDto query)
    {
        var requestQuery = _context.CustomerActionRequests.AsNoTracking();
        if (query.OrderId.HasValue)
        {
            requestQuery = requestQuery.Where(x => x.OrderId == query.OrderId.Value);
        }

        if (query.FromUtc.HasValue)
        {
            requestQuery = requestQuery.Where(
                x => x.RequestedAt >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            requestQuery = requestQuery.Where(
                x => x.RequestedAt <= query.ToUtc.Value);
        }

        if (query.Status.HasValue)
        {
            requestQuery = requestQuery.Where(
                x => x.Order.Status == query.Status.Value);
        }

        var requestRows = await requestQuery
            .Select(x => new
            {
                EventId = x.Id,
                x.OrderId,
                x.Order.OrderNumber,
                CustomerName = x.Order.User.FullName,
                CustomerEmail = x.Order.User.Email,
                AssignedAgentName = x.Order.AssignedAgent == null
                    ? null
                    : x.Order.AssignedAgent.FullName,
                OccurredAt = x.RequestedAt,
                x.RequestText,
                ActorName = x.RequestedByAgent.FullName,
                ActorEmail = x.RequestedByAgent.Email
            })
            .ToListAsync();

        events.AddRange(requestRows.Select(x => new AuditEventProjection
        {
            EventId = x.EventId,
            OrderId = x.OrderId,
            OrderNumber = x.OrderNumber,
            CustomerName = SafeName(x.CustomerName, x.CustomerEmail),
            CustomerEmail = x.CustomerEmail,
            AssignedAgentName = x.AssignedAgentName,
            OccurredAt = x.OccurredAt,
            EventType = "CustomerAction",
            Title = "Customer action requested",
            Description = x.RequestText,
            ActorType = OrderLogActorType.Agent.ToString(),
            ActorName = ActorName(
                x.ActorName,
                x.ActorEmail,
                OrderLogActorType.Agent.ToString()),
            ActorEmail = x.ActorEmail,
            Tone = "warning"
        }));

        var responseQuery = _context.CustomerActionRequests
            .AsNoTracking()
            .Where(x => x.RespondedAt.HasValue);
        if (query.OrderId.HasValue)
        {
            responseQuery = responseQuery.Where(x => x.OrderId == query.OrderId.Value);
        }

        if (query.FromUtc.HasValue)
        {
            responseQuery = responseQuery.Where(
                x => x.RespondedAt >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            responseQuery = responseQuery.Where(
                x => x.RespondedAt <= query.ToUtc.Value);
        }

        if (query.Status.HasValue)
        {
            responseQuery = responseQuery.Where(
                x => x.Order.Status == query.Status.Value);
        }

        var responseRows = await responseQuery
            .Select(x => new
            {
                EventId = x.Id,
                x.OrderId,
                x.Order.OrderNumber,
                CustomerName = x.Order.User.FullName,
                CustomerEmail = x.Order.User.Email,
                AssignedAgentName = x.Order.AssignedAgent == null
                    ? null
                    : x.Order.AssignedAgent.FullName,
                OccurredAt = x.RespondedAt!.Value,
                x.CustomerResponse
            })
            .ToListAsync();

        events.AddRange(responseRows.Select(x => new AuditEventProjection
        {
            EventId = x.EventId,
            OrderId = x.OrderId,
            OrderNumber = x.OrderNumber,
            CustomerName = SafeName(x.CustomerName, x.CustomerEmail),
            CustomerEmail = x.CustomerEmail,
            AssignedAgentName = x.AssignedAgentName,
            OccurredAt = x.OccurredAt,
            EventType = "CustomerAction",
            Title = "Customer response recorded",
            Description = x.CustomerResponse ?? "Customer responded.",
            ActorType = OrderLogActorType.User.ToString(),
            ActorName = SafeName(x.CustomerName, x.CustomerEmail),
            ActorEmail = x.CustomerEmail,
            Tone = "success"
        }));
    }

    private static IEnumerable<AuditEventProjection> ApplyTextFilters(
        IEnumerable<AuditEventProjection> events,
        AdminAuditQueryDto query)
    {
        if (!string.IsNullOrWhiteSpace(query.Actor))
        {
            var actor = query.Actor.Trim();
            events = events.Where(x =>
                Contains(x.ActorName, actor) ||
                Contains(x.ActorEmail, actor) ||
                Contains(x.ActorType, actor));
        }

        if (!string.IsNullOrWhiteSpace(query.Order))
        {
            var order = query.Order.Trim();
            events = events.Where(x =>
                Contains(x.OrderNumber, order) ||
                Contains(x.OrderId.ToString(), order));
        }

        return events;
    }

    private static AdminAuditEventDto ToAuditDto(AuditEventProjection value)
    {
        return new AdminAuditEventDto
        {
            EventId = value.EventId,
            OrderId = value.OrderId,
            OrderNumber = value.OrderNumber,
            CustomerName = value.CustomerName,
            CustomerEmail = value.CustomerEmail,
            AssignedAgentName = value.AssignedAgentName,
            OccurredAt = value.OccurredAt,
            EventType = value.EventType,
            Title = value.Title,
            Description = value.Description,
            Status = value.Status,
            ActorType = value.ActorType,
            ActorName = value.ActorName,
            ActorEmail = value.ActorEmail,
            Tone = value.Tone
        };
    }

    private static AdminOrderTimelineEventDto ToTimelineDto(
        AuditEventProjection value)
    {
        return new AdminOrderTimelineEventDto
        {
            EventId = value.EventId,
            OccurredAt = value.OccurredAt,
            EventType = value.EventType,
            Title = value.Title,
            Description = value.Description,
            Status = value.Status,
            ActorType = value.ActorType,
            ActorName = value.ActorName,
            ActorEmail = value.ActorEmail,
            Tone = value.Tone
        };
    }

    private static bool ShouldLoadEventType(string? filter, string eventType)
    {
        return string.IsNullOrWhiteSpace(filter) ||
            Contains(eventType, filter.Trim()) ||
            Contains(filter.Trim(), eventType);
    }

    private static bool Contains(string? value, string term)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            value.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static string SafeName(string? name, string? email)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return string.IsNullOrWhiteSpace(email) ? "Unknown" : email;
    }

    private static string ActorName(
        string? name,
        string? email,
        string actorType)
    {
        var safeName = SafeName(name, email);
        return safeName == "Unknown" ? actorType : safeName;
    }

    private static string PaymentDescription(string? gatewayReference, string? note)
    {
        if (!string.IsNullOrWhiteSpace(note))
        {
            return note;
        }

        return string.IsNullOrWhiteSpace(gatewayReference)
            ? "Payment event recorded."
            : $"Gateway reference {gatewayReference}.";
    }

    private static string FormatStatus(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character)
                ? $" {character}"
                : character.ToString()));
    }

    private static string OrderTone(string status)
    {
        return status switch
        {
            nameof(OrderStatus.Paid) or nameof(OrderStatus.Completed) => "success",
            nameof(OrderStatus.Cancelled) or nameof(OrderStatus.Failed) => "danger",
            nameof(OrderStatus.Created) or nameof(OrderStatus.WaitingForPayment) => "warning",
            nameof(OrderStatus.Assigned) or nameof(OrderStatus.Processing) => "info",
            _ => "neutral"
        };
    }

    private static string PaymentTone(string eventType)
    {
        return eventType switch
        {
            nameof(PaymentEventType.Completed) => "success",
            nameof(PaymentEventType.Failed) or nameof(PaymentEventType.Refunded) => "danger",
            nameof(PaymentEventType.Initiated) => "warning",
            _ => "neutral"
        };
    }

    private static string RefundTone(string status)
    {
        return status switch
        {
            nameof(RefundStatus.Approved) or nameof(RefundStatus.Completed) => "success",
            nameof(RefundStatus.Rejected) or nameof(RefundStatus.Cancelled) => "danger",
            nameof(RefundStatus.Requested) => "warning",
            _ => "neutral"
        };
    }

    private sealed class AuditEventProjection
    {
        public Guid EventId { get; set; }
        public Guid OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string? AssignedAgentName { get; set; }
        public DateTime OccurredAt { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Status { get; set; }
        public string ActorType { get; set; } = string.Empty;
        public string ActorName { get; set; } = string.Empty;
        public string? ActorEmail { get; set; }
        public string Tone { get; set; } = "neutral";
    }
}
