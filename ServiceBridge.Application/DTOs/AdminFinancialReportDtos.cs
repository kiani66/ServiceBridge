using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public class AdminFinancialReportFilterDto
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public RefundStatus? RefundStatus { get; set; }
}

public class AdminFinancialReportDto
{
    public AdminPaymentSummaryReportDto Payments { get; set; } = new();
    public AdminOrderFinancialSummaryReportDto Orders { get; set; } = new();
    public AdminRefundSummaryReportDto Refunds { get; set; } = new();
}

public class AdminPaymentSummaryReportDto
{
    public int TotalPayments { get; set; }
    public int CompletedPayments { get; set; }
    public int FailedPayments { get; set; }
    public int PendingPayments { get; set; }
    public int RefundedPayments { get; set; }
    public decimal TotalAmountIrr { get; set; }
    public IReadOnlyCollection<AdminPaymentReportRowDto> Rows { get; set; } =
        Array.Empty<AdminPaymentReportRowDto>();
}

public class AdminPaymentReportRowDto
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal AmountIrr { get; set; }
    public DateTime PaymentDate { get; set; }
}

public class AdminOrderFinancialSummaryReportDto
{
    public int OrderCount { get; set; }
    public decimal TotalFinalPriceIrr { get; set; }
    public decimal PaidAmountIrr { get; set; }
    public decimal RefundedAmountIrr { get; set; }
    public decimal EstimatedProfitIrr { get; set; }
    public IReadOnlyCollection<AdminOrderFinancialReportRowDto> Rows { get; set; } =
        Array.Empty<AdminOrderFinancialReportRowDto>();
}

public class AdminOrderFinancialReportRowDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal FinalPriceIrr { get; set; }
    public decimal PaidAmountIrr { get; set; }
    public decimal RefundedAmountIrr { get; set; }
    public decimal EstimatedProfitIrr { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminRefundSummaryReportDto
{
    public int RefundCount { get; set; }
    public int RequestedRefunds { get; set; }
    public int ApprovedRefunds { get; set; }
    public int RejectedRefunds { get; set; }
    public int CompletedRefunds { get; set; }
    public int CancelledRefunds { get; set; }
    public decimal TotalRefundAmountIrr { get; set; }
    public IReadOnlyCollection<AdminRefundReportRowDto> Rows { get; set; } =
        Array.Empty<AdminRefundReportRowDto>();
}

public class AdminRefundReportRowDto
{
    public Guid RefundRequestId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal AmountIrr { get; set; }
    public DateTime RequestedAtUtc { get; set; }
}

public class AdminFinancialCsvExportDto
{
    public string FileName { get; set; } = "admin-financial-report.csv";
    public string ContentType { get; set; } = "text/csv";
    public string Content { get; set; } = string.Empty;
}
