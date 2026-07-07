using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;

namespace ServiceBridge.Application.Interfaces;

public interface IAdminFinancialReportService
{
    Task<ServiceResult<AdminFinancialReportDto>> GetFinancialReportAsync(
        AdminFinancialReportFilterDto filter);
    Task<ServiceResult<AdminPaymentSummaryReportDto>> GetPaymentSummaryAsync(
        AdminFinancialReportFilterDto filter);
    Task<ServiceResult<AdminRefundSummaryReportDto>> GetRefundSummaryAsync(
        AdminFinancialReportFilterDto filter);
    Task<ServiceResult<AdminFinancialCsvExportDto>> ExportFinancialReportCsvAsync(
        AdminFinancialReportFilterDto filter);
}
