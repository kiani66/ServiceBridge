using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.Interfaces;

namespace ServiceBridge.Api.Controllers;

[AllowAnonymous]
[Route("api/services/{serviceId:guid}/quote")]
public class PricingController : BaseApiController
{
    private readonly IPricingQuoteService _pricingQuoteService;

    public PricingController(IPricingQuoteService pricingQuoteService)
    {
        _pricingQuoteService = pricingQuoteService;
    }

    [HttpGet]
    public async Task<IActionResult> GetQuote(
        Guid serviceId,
        [FromQuery] Guid? planId)
    {
        var result = await _pricingQuoteService.GetQuoteAsync(
            serviceId,
            planId,
            DateTime.UtcNow);

        return ToActionResult(result);
    }
}
