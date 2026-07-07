using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Infrastructure.Reports;

internal static class AdminFinancialReportCalculator
{
    public static AdminPaymentSummaryReportDto BuildPaymentSummary(
        IReadOnlyCollection<AdminPaymentReportRecord> records)
    {
        return new AdminPaymentSummaryReportDto
        {
            TotalPayments = records.Count,
            CompletedPayments = records.Count(x => x.Status == PaymentStatus.Completed),
            FailedPayments = records.Count(x => x.Status == PaymentStatus.Failed),
            PendingPayments = records.Count(x => x.Status == PaymentStatus.Pending),
            RefundedPayments = records.Count(x => x.Status == PaymentStatus.Refunded),
            TotalAmountIrr = records.Sum(x => x.AmountIrr),
            Rows = records
                .OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.PaymentId)
                .Select(x => new AdminPaymentReportRowDto
                {
                    PaymentId = x.PaymentId,
                    OrderId = x.OrderId,
                    OrderNumber = x.OrderNumber,
                    CustomerName = x.CustomerName,
                    Status = x.Status.ToString(),
                    AmountIrr = x.AmountIrr,
                    PaymentDate = x.PaymentDate
                })
                .ToList()
        };
    }

    public static AdminOrderFinancialSummaryReportDto BuildOrderSummary(
        IReadOnlyCollection<AdminOrderFinancialReportRecord> records)
    {
        return new AdminOrderFinancialSummaryReportDto
        {
            OrderCount = records.Count,
            TotalFinalPriceIrr = records.Sum(x => x.FinalPriceIrr),
            PaidAmountIrr = records.Sum(x => x.PaidAmountIrr),
            RefundedAmountIrr = records.Sum(x => x.RefundedAmountIrr),
            EstimatedProfitIrr = records.Sum(x => x.EstimatedProfitIrr),
            Rows = records
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.OrderId)
                .Select(x => new AdminOrderFinancialReportRowDto
                {
                    OrderId = x.OrderId,
                    OrderNumber = x.OrderNumber,
                    CustomerName = x.CustomerName,
                    Status = x.Status.ToString(),
                    FinalPriceIrr = x.FinalPriceIrr,
                    PaidAmountIrr = x.PaidAmountIrr,
                    RefundedAmountIrr = x.RefundedAmountIrr,
                    EstimatedProfitIrr = x.EstimatedProfitIrr,
                    CreatedAt = x.CreatedAt
                })
                .ToList()
        };
    }

    public static AdminRefundSummaryReportDto BuildRefundSummary(
        IReadOnlyCollection<AdminRefundReportRecord> records)
    {
        return new AdminRefundSummaryReportDto
        {
            RefundCount = records.Count,
            RequestedRefunds = records.Count(x => x.Status == RefundStatus.Requested),
            ApprovedRefunds = records.Count(x => x.Status == RefundStatus.Approved),
            RejectedRefunds = records.Count(x => x.Status == RefundStatus.Rejected),
            CompletedRefunds = records.Count(x => x.Status == RefundStatus.Completed),
            CancelledRefunds = records.Count(x => x.Status == RefundStatus.Cancelled),
            TotalRefundAmountIrr = records.Sum(x => x.AmountIrr),
            Rows = records
                .OrderByDescending(x => x.RequestedAtUtc)
                .ThenByDescending(x => x.RefundRequestId)
                .Select(x => new AdminRefundReportRowDto
                {
                    RefundRequestId = x.RefundRequestId,
                    OrderId = x.OrderId,
                    OrderNumber = x.OrderNumber,
                    CustomerName = x.CustomerName,
                    Status = x.Status.ToString(),
                    AmountIrr = x.AmountIrr,
                    RequestedAtUtc = x.RequestedAtUtc
                })
                .ToList()
        };
    }
}

internal sealed record AdminPaymentReportRecord(
    Guid PaymentId,
    Guid OrderId,
    string OrderNumber,
    string CustomerName,
    PaymentStatus Status,
    decimal AmountIrr,
    DateTime PaymentDate);

internal sealed record AdminOrderFinancialReportRecord(
    Guid OrderId,
    string OrderNumber,
    string CustomerName,
    OrderStatus Status,
    decimal FinalPriceIrr,
    decimal PaidAmountIrr,
    decimal RefundedAmountIrr,
    decimal EstimatedProfitIrr,
    DateTime CreatedAt);

internal sealed record AdminRefundReportRecord(
    Guid RefundRequestId,
    Guid OrderId,
    string OrderNumber,
    string CustomerName,
    RefundStatus Status,
    decimal AmountIrr,
    DateTime RequestedAtUtc);
