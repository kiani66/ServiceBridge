using Microsoft.EntityFrameworkCore;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Data;
using ServiceBridge.Infrastructure.Reports;

namespace ServiceBridge.Infrastructure.Repositories;

public class AdminFinancialReportRepository : IAdminFinancialReportRepository
{
    private readonly ServiceBridgeDbContext _context;

    public AdminFinancialReportRepository(ServiceBridgeDbContext context)
    {
        _context = context;
    }

    public async Task<AdminPaymentSummaryReportDto> GetPaymentSummaryAsync(
        AdminFinancialReportFilterDto filter)
    {
        var query = _context.Payments.AsNoTracking();
        query = ApplyPaymentFilters(query, filter);

        var records = await query
            .Select(x => new AdminPaymentReportRecord(
                x.Id,
                x.OrderId,
                x.Order.OrderNumber,
                x.Order.User.FullName,
                x.Status,
                x.AmountIRR,
                x.ConfirmedAt ?? x.FailedAt ?? x.RefundedAt ?? x.CreatedAt))
            .ToListAsync();

        return AdminFinancialReportCalculator.BuildPaymentSummary(records);
    }

    public async Task<AdminOrderFinancialSummaryReportDto>
        GetOrderFinancialSummaryAsync(AdminFinancialReportFilterDto filter)
    {
        var query = ApplyOrderFilters(_context.Orders.AsNoTracking(), filter);

        var records = await query
            .Select(x => new AdminOrderFinancialReportRecord(
                x.Id,
                x.OrderNumber,
                x.User.FullName,
                x.Status,
                x.FinalAmountIrr,
                x.PaymentRecords
                    .Where(payment =>
                        payment.Status == PaymentStatus.Completed ||
                        payment.Status == PaymentStatus.Refunded)
                    .Sum(payment => (decimal?)payment.AmountIRR) ?? 0m,
                x.PaymentRecords
                    .Where(payment => payment.Status == PaymentStatus.Refunded)
                    .Sum(payment => (decimal?)payment.AmountIRR) ?? 0m,
                x.ProfitAmountIrr,
                x.CreatedAt))
            .ToListAsync();

        return AdminFinancialReportCalculator.BuildOrderSummary(records);
    }

    public async Task<AdminRefundSummaryReportDto> GetRefundSummaryAsync(
        AdminFinancialReportFilterDto filter)
    {
        var query = ApplyRefundFilters(_context.RefundRequests.AsNoTracking(), filter);
        var records = await query
            .Select(x => new AdminRefundReportRecord(
                x.Id,
                x.OrderId,
                x.Order.OrderNumber,
                x.Order.User.FullName,
                x.Status,
                x.Amount,
                x.CreatedAt))
            .ToListAsync();

        return AdminFinancialReportCalculator.BuildRefundSummary(records);
    }

    internal static IQueryable<ServiceBridge.Domain.Entities.Order>
        ApplyOrderFilters(
            IQueryable<ServiceBridge.Domain.Entities.Order> query,
            AdminFinancialReportFilterDto filter)
    {
        if (filter.FromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= filter.ToUtc.Value);
        }

        if (filter.PaymentStatus.HasValue)
        {
            query = query.Where(x => x.PaymentRecords.Any(payment =>
                payment.Status == filter.PaymentStatus.Value));
        }

        return query;
    }

    internal static IQueryable<ServiceBridge.Domain.Entities.RefundRequest>
        ApplyRefundFilters(
            IQueryable<ServiceBridge.Domain.Entities.RefundRequest> query,
            AdminFinancialReportFilterDto filter)
    {
        if (filter.FromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= filter.ToUtc.Value);
        }

        if (filter.RefundStatus.HasValue)
        {
            query = query.Where(x => x.Status == filter.RefundStatus.Value);
        }

        return query;
    }

    internal static IQueryable<ServiceBridge.Domain.Entities.Payment>
        ApplyPaymentFilters(
            IQueryable<ServiceBridge.Domain.Entities.Payment> query,
            AdminFinancialReportFilterDto filter)
    {
        if (filter.FromUtc.HasValue)
        {
            query = query.Where(x =>
                (x.ConfirmedAt ?? x.FailedAt ?? x.RefundedAt ?? x.CreatedAt) >=
                filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(x =>
                (x.ConfirmedAt ?? x.FailedAt ?? x.RefundedAt ?? x.CreatedAt) <=
                filter.ToUtc.Value);
        }

        if (filter.PaymentStatus.HasValue)
        {
            query = query.Where(x => x.Status == filter.PaymentStatus.Value);
        }

        return query;
    }
}
