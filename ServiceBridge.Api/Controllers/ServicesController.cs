using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Api.Controllers;

[AllowAnonymous]
[Route("api/services")]
public class ServicesController : BaseApiController
{
    private readonly IServiceCatalogQueryService _serviceCatalogQueryService;

    public ServicesController(IServiceCatalogQueryService serviceCatalogQueryService)
    {
        _serviceCatalogQueryService = serviceCatalogQueryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveServices()
    {
        var result = await _serviceCatalogQueryService.GetActiveServicesAsync();
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetActiveService(Guid id)
    {
        var result = await _serviceCatalogQueryService.GetActiveServiceByIdAsync(id);
        return ToActionResult(result);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetActiveServiceBySlug(string slug)
    {
        var result = await _serviceCatalogQueryService.GetActiveServiceBySlugAsync(slug);
        return ToActionResult(result);
    }
}
