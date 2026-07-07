using ServiceBridge.Application.DTOs;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminFinancialReportRepository
{
    Task<AdminPaymentSummaryReportDto> GetPaymentSummaryAsync(
        AdminFinancialReportFilterDto filter);
    Task<AdminOrderFinancialSummaryReportDto> GetOrderFinancialSummaryAsync(
        AdminFinancialReportFilterDto filter);
    Task<AdminRefundSummaryReportDto> GetRefundSummaryAsync(
        AdminFinancialReportFilterDto filter);
}
