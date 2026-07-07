using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.Admin)]
[Route("api/admin/reports")]
public class AdminReportsController : BaseApiController
{
    private readonly IAdminFinancialReportService _reportService;

    public AdminReportsController(IAdminFinancialReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("financial")]
    public async Task<IActionResult> GetFinancialReport(
        [FromQuery] AdminFinancialReportFilterDto filter)
    {
        return ToActionResult(await _reportService.GetFinancialReportAsync(filter));
    }

    [HttpGet("financial.csv")]
    public async Task<IActionResult> ExportFinancialReportCsv(
        [FromQuery] AdminFinancialReportFilterDto filter)
    {
        var result = await _reportService.ExportFinancialReportCsvAsync(filter);
        if (!result.Success || result.Data == null)
        {
            return ToActionResult(result);
        }

        return File(
            Encoding.UTF8.GetBytes(result.Data.Content),
            result.Data.ContentType,
            result.Data.FileName);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPaymentSummary(
        [FromQuery] AdminFinancialReportFilterDto filter)
    {
        return ToActionResult(await _reportService.GetPaymentSummaryAsync(filter));
    }

    [HttpGet("refunds")]
    public async Task<IActionResult> GetRefundSummary(
        [FromQuery] AdminFinancialReportFilterDto filter)
    {
        return ToActionResult(await _reportService.GetRefundSummaryAsync(filter));
    }
}
