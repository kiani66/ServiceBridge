using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.Admin)]
[Route("api/admin/catalog")]
public class AdminCatalogController : BaseApiController
{
    private readonly IServiceCatalogQueryService _catalogQueryService;
    private readonly IAdminCatalogService _adminCatalogService;

    public AdminCatalogController(
        IServiceCatalogQueryService catalogQueryService,
        IAdminCatalogService adminCatalogService)
    {
        _catalogQueryService = catalogQueryService;
        _adminCatalogService = adminCatalogService;
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _catalogQueryService.GetAllCategoriesAsync();
        return ToActionResult(result);
    }

    [HttpGet("services")]
    public async Task<IActionResult> GetServices()
    {
        var result = await _catalogQueryService.GetAllServicesAsync();
        return ToActionResult(result);
    }

    [HttpGet("services/{id:guid}")]
    public async Task<IActionResult> GetService(Guid id)
    {
        var result = await _catalogQueryService.GetServiceByIdAsync(id);
        return ToActionResult(result);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateServiceCategoryRequest request)
    {
        var result = await _adminCatalogService.CreateCategoryAsync(request);
        return ToActionResult(result);
    }

    [HttpPut("categories/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateServiceCategoryRequest request)
    {
        var result = await _adminCatalogService.UpdateCategoryAsync(id, request);
        return ToActionResult(result);
    }

    [HttpPost("services")]
    public async Task<IActionResult> CreateService(
        [FromBody] CreateDigitalServiceRequest request)
    {
        var result = await _adminCatalogService.CreateServiceAsync(request);
        return ToActionResult(result);
    }

    [HttpPut("services/{id:guid}")]
    public async Task<IActionResult> UpdateService(
        Guid id,
        [FromBody] UpdateDigitalServiceRequest request)
    {
        var result = await _adminCatalogService.UpdateServiceAsync(id, request);
        return ToActionResult(result);
    }

    [HttpPost("services/{serviceId:guid}/plans")]
    public async Task<IActionResult> CreateVariant(
        Guid serviceId,
        [FromBody] CreateServiceVariantRequest request)
    {
        var result = await _adminCatalogService.CreateVariantAsync(serviceId, request);
        return ToActionResult(result);
    }

    [HttpPut("plans/{id:guid}")]
    public async Task<IActionResult> UpdateVariant(
        Guid id,
        [FromBody] UpdateServiceVariantRequest request)
    {
        var result = await _adminCatalogService.UpdateVariantAsync(id, request);
        return ToActionResult(result);
    }

    [HttpPost("services/{serviceId:guid}/fields")]
    public async Task<IActionResult> CreateRequiredField(
        Guid serviceId,
        [FromBody] CreateServiceRequiredFieldRequest request)
    {
        var result = await _adminCatalogService.CreateRequiredFieldAsync(serviceId, request);
        return ToActionResult(result);
    }

    [HttpPut("fields/{id:guid}")]
    public async Task<IActionResult> UpdateRequiredField(
        Guid id,
        [FromBody] UpdateServiceRequiredFieldRequest request)
    {
        var result = await _adminCatalogService.UpdateRequiredFieldAsync(id, request);
        return ToActionResult(result);
    }
}
