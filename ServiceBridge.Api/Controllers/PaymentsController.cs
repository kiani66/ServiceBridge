using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

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
        if (!Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
        {
            return ToActionResult(ServiceResult.Fail(
                AppMessages.UnauthorizedAccess,
                AppConstants.ErrorCodes.Unauthorized,
                401));
        }

        var result = await _paymentService.InitiateAsync(userId, request);
        return ToActionResult(result);
    }

    [Authorize(Roles = AppConstants.Roles.User)]
    [HttpPost("mock")]
    public async Task<IActionResult> Mock(
        [FromBody] MockPaymentRequest request)
    {
        if (!Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId))
        {
            return ToActionResult(ServiceResult.Fail(
                AppMessages.UnauthorizedAccess,
                AppConstants.ErrorCodes.Unauthorized,
                401));
        }

        var result = await _paymentService.ProcessMockAsync(userId, request);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("callback")]
    public async Task<IActionResult> Callback(
        [FromBody] JsonElement callbackPayload)
    {
        ConfirmPaymentRequest? request;
        try
        {
            request = callbackPayload.Deserialize<ConfirmPaymentRequest>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return ToActionResult(ServiceResult.ValidationFailed(
                new Dictionary<string, string[]>
                {
                    ["body"] = ["The callback payload is invalid."]
                }));
        }

        if (request == null)
        {
            return ToActionResult(ServiceResult.ValidationFailed(
                new Dictionary<string, string[]>
                {
                    ["body"] = ["The callback payload is required."]
                }));
        }

        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                validationResults,
                validateAllProperties: true))
        {
            var errors = validationResults
                .SelectMany(
                    result => result.MemberNames.DefaultIfEmpty("body"),
                    (result, memberName) => new
                    {
                        MemberName = memberName,
                        Message = result.ErrorMessage ?? AppMessages.ValidationFailed
                    })
                .GroupBy(x => x.MemberName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(x => x.Message).ToArray());

            return ToActionResult(ServiceResult.ValidationFailed(errors));
        }

        var result = await _paymentService.ConfirmAsync(
            request,
            callbackPayload.GetRawText());
        return ToActionResult(result);
    }
}
