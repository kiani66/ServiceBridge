using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Payments;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Route("api/payments")]
public class PaymentsController : BaseApiController
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [Authorize(Roles = AppConstants.Roles.User)]
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate(
        [FromBody] InitiatePaymentRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        var result = await _paymentService.InitiateAsync(userId, request);
        return ToActionResult(result);
    }

    [Authorize(Roles = AppConstants.Roles.User)]
    [HttpPost("mock")]
    public async Task<IActionResult> Mock(
        [FromBody] MockPaymentRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        var result = await _paymentService.ProcessMockAsync(userId, request);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("callback")]
    public async Task<IActionResult> Callback(
        [FromBody] System.Text.Json.JsonElement callbackPayload,
        [FromQuery] string? provider = null)
    {
        if (callbackPayload.ValueKind is System.Text.Json.JsonValueKind.Undefined)
        {
            return ToActionResult(ServiceResult.ValidationFailed(
                new Dictionary<string, string[]>
                {
                    ["body"] = ["The callback payload is required."]
                }));
        }

        var result = await _paymentService.VerifyCallbackAsync(
            new PaymentGatewayVerifyRequest(
                provider,
                callbackPayload.GetRawText()));
        return ToActionResult(result);
    }
}
