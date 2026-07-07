using System.Globalization;
using System.Text;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class AdminFinancialReportService : IAdminFinancialReportService
{
    private readonly IAdminFinancialReportRepository _repository;

    public AdminFinancialReportService(IAdminFinancialReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<AdminFinancialReportDto>> GetFinancialReportAsync(
        AdminFinancialReportFilterDto filter)
    {
        var validation = ValidateFilter<AdminFinancialReportDto>(filter);
        if (validation != null)
        {
            return validation;
        }

        return ServiceResult<AdminFinancialReportDto>.Ok(new AdminFinancialReportDto
        {
            Payments = await _repository.GetPaymentSummaryAsync(filter),
            Orders = await _repository.GetOrderFinancialSummaryAsync(filter),
            Refunds = await _repository.GetRefundSummaryAsync(filter)
        });
    }

    public async Task<ServiceResult<AdminPaymentSummaryReportDto>>
        GetPaymentSummaryAsync(AdminFinancialReportFilterDto filter)
    {
        var validation = ValidateFilter<AdminPaymentSummaryReportDto>(filter);
        if (validation != null)
        {
            return validation;
        }

        return ServiceResult<AdminPaymentSummaryReportDto>.Ok(
            await _repository.GetPaymentSummaryAsync(filter));
    }

    public async Task<ServiceResult<AdminRefundSummaryReportDto>>
        GetRefundSummaryAsync(AdminFinancialReportFilterDto filter)
    {
        var validation = ValidateFilter<AdminRefundSummaryReportDto>(filter);
        if (validation != null)
        {
            return validation;
        }

        return ServiceResult<AdminRefundSummaryReportDto>.Ok(
            await _repository.GetRefundSummaryAsync(filter));
    }

    public async Task<ServiceResult<AdminFinancialCsvExportDto>>
        ExportFinancialReportCsvAsync(AdminFinancialReportFilterDto filter)
    {
        var reportResult = await GetFinancialReportAsync(filter);
        if (!reportResult.Success || reportResult.Data == null)
        {
            return ServiceResult<AdminFinancialCsvExportDto>.Fail(
                reportResult.Message ?? AppMessages.ValidationFailed,
                reportResult.ErrorCode ?? AppConstants.ErrorCodes.ValidationFailed,
                reportResult.StatusCode);
        }

        return ServiceResult<AdminFinancialCsvExportDto>.Ok(new AdminFinancialCsvExportDto
        {
            FileName = $"admin-financial-report-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
            Content = BuildCsv(reportResult.Data)
        });
    }

    private static ServiceResult<T>? ValidateFilter<T>(
        AdminFinancialReportFilterDto filter)
    {
        if (filter.FromUtc.HasValue &&
            filter.ToUtc.HasValue &&
            filter.FromUtc.Value > filter.ToUtc.Value)
        {
            return new ServiceResult<T>
            {
                Success = false,
                Message = AppMessages.ValidationFailed,
                ErrorCode = AppConstants.ErrorCodes.ValidationFailed,
                StatusCode = 400,
                ValidationErrors = new Dictionary<string, string[]>
                {
                    ["fromUtc"] = new[] { "The start date must be before the end date." }
                }
            };
        }

        return null;
    }

    private static string BuildCsv(AdminFinancialReportDto report)
    {
        var builder = new StringBuilder();
        AppendLine(builder, "Section", "Metric", "Value");
        AppendLine(builder, "Payments", "Total payments", report.Payments.TotalPayments);
        AppendLine(builder, "Payments", "Completed payments", report.Payments.CompletedPayments);
        AppendLine(builder, "Payments", "Failed payments", report.Payments.FailedPayments);
        AppendLine(builder, "Payments", "Pending payments", report.Payments.PendingPayments);
        AppendLine(builder, "Payments", "Refunded payments", report.Payments.RefundedPayments);
        AppendLine(builder, "Payments", "Total amount IRR", report.Payments.TotalAmountIrr);
        AppendLine(builder, "Orders", "Order count", report.Orders.OrderCount);
        AppendLine(builder, "Orders", "Total final price IRR", report.Orders.TotalFinalPriceIrr);
        AppendLine(builder, "Orders", "Paid amount IRR", report.Orders.PaidAmountIrr);
        AppendLine(builder, "Orders", "Refunded amount IRR", report.Orders.RefundedAmountIrr);
        AppendLine(builder, "Orders", "Estimated profit IRR", report.Orders.EstimatedProfitIrr);
        AppendLine(builder, "Refunds", "Refund count", report.Refunds.RefundCount);
        AppendLine(builder, "Refunds", "Requested refunds", report.Refunds.RequestedRefunds);
        AppendLine(builder, "Refunds", "Approved refunds", report.Refunds.ApprovedRefunds);
        AppendLine(builder, "Refunds", "Rejected refunds", report.Refunds.RejectedRefunds);
        AppendLine(builder, "Refunds", "Completed refunds", report.Refunds.CompletedRefunds);
        AppendLine(builder, "Refunds", "Cancelled refunds", report.Refunds.CancelledRefunds);
        AppendLine(builder, "Refunds", "Total refund amount IRR", report.Refunds.TotalRefundAmountIrr);

        builder.AppendLine();
        AppendLine(builder, "PaymentId", "OrderNumber", "Customer", "Status", "AmountIRR", "PaymentDateUtc");
        foreach (var row in report.Payments.Rows)
        {
            AppendLine(builder, row.PaymentId, row.OrderNumber, row.CustomerName, row.Status, row.AmountIrr, row.PaymentDate);
        }

        builder.AppendLine();
        AppendLine(builder, "OrderId", "OrderNumber", "Customer", "Status", "FinalPriceIRR", "PaidAmountIRR", "RefundedAmountIRR", "EstimatedProfitIRR", "CreatedAtUtc");
        foreach (var row in report.Orders.Rows)
        {
            AppendLine(builder, row.OrderId, row.OrderNumber, row.CustomerName, row.Status, row.FinalPriceIrr, row.PaidAmountIrr, row.RefundedAmountIrr, row.EstimatedProfitIrr, row.CreatedAt);
        }

        builder.AppendLine();
        AppendLine(builder, "RefundRequestId", "OrderNumber", "Customer", "Status", "AmountIRR", "RequestedAtUtc");
        foreach (var row in report.Refunds.Rows)
        {
            AppendLine(builder, row.RefundRequestId, row.OrderNumber, row.CustomerName, row.Status, row.AmountIrr, row.RequestedAtUtc);
        }

        return builder.ToString();
    }

    private static void AppendLine(StringBuilder builder, params object?[] values)
    {
        builder.AppendLine(string.Join(",", values.Select(Escape)));
    }

    private static string Escape(object? value)
    {
        var text = value switch
        {
            null => string.Empty,
            DateTime dateTime => dateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

        return text.Contains('"') || text.Contains(',') || text.Contains('\n') || text.Contains('\r')
            ? $"\"{text.Replace("\"", "\"\"")}\""
            : text;
    }
}
