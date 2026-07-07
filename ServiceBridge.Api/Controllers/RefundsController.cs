using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Refunds;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.User)]
[Route("api/orders/my/{orderId:guid}/refunds")]
public class RefundsController : BaseApiController
{
    private readonly CreateRefundRequest _createRefundRequest;
    private readonly GetUserRefundHistoryQuery _getUserRefundHistoryQuery;

    public RefundsController(
        CreateRefundRequest createRefundRequest,
        GetUserRefundHistoryQuery getUserRefundHistoryQuery)
    {
        _createRefundRequest = createRefundRequest;
        _getUserRefundHistoryQuery = getUserRefundHistoryQuery;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid orderId,
        [FromBody] CreateRefundRequestRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _createRefundRequest.ExecuteAsync(
            userId,
            orderId,
            request));
    }

    [HttpGet]
    public async Task<IActionResult> GetForUser(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetUserRefundHistoryQuery.DefaultPageSize)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _getUserRefundHistoryQuery.ExecuteAsync(
            userId,
            page,
            pageSize));
    }
}
