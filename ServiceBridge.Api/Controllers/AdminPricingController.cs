using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.Admin)]
[Route("api/admin/pricing")]
public class AdminPricingController : BaseApiController
{
    private readonly IAdminPricingQueryService _queryService;
    private readonly IAdminPricingService _adminService;

    public AdminPricingController(
        IAdminPricingQueryService queryService,
        IAdminPricingService adminService)
    {
        _queryService = queryService;
        _adminService = adminService;
    }

    [HttpGet("exchange-rates")]
    public async Task<IActionResult> GetExchangeRates([FromQuery] string? currencyCode)
    {
        var result = await _queryService.GetExchangeRatesAsync(currencyCode);
        return ToActionResult(result);
    }

    [HttpPost("exchange-rates")]
    public async Task<IActionResult> CreateExchangeRate(
        [FromBody] CreateExchangeRateRequest request)
    {
        var result = await _adminService.CreateExchangeRateAsync(request);
        return ToActionResult(result);
    }

    [HttpPatch("exchange-rates/{id:guid}/status")]
    public async Task<IActionResult> UpdateExchangeRateStatus(
        Guid id,
        [FromBody] UpdatePricingStatusRequest request)
    {
        var result = await _adminService.UpdateExchangeRateStatusAsync(id, request);
        return ToActionResult(result);
    }

    [HttpGet("profit-margins")]
    public async Task<IActionResult> GetProfitMargins()
    {
        var result = await _queryService.GetProfitMarginsAsync();
        return ToActionResult(result);
    }

    [HttpPost("profit-margins")]
    public async Task<IActionResult> CreateProfitMargin(
        [FromBody] CreateProfitMarginRequest request)
    {
        var result = await _adminService.CreateProfitMarginAsync(request);
        return ToActionResult(result);
    }

    [HttpPatch("profit-margins/{id:guid}/status")]
    public async Task<IActionResult> UpdateProfitMarginStatus(
        Guid id,
        [FromBody] UpdatePricingStatusRequest request)
    {
        var result = await _adminService.UpdateProfitMarginStatusAsync(id, request);
        return ToActionResult(result);
    }
}
