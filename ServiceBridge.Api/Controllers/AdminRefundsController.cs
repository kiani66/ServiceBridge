using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Refunds;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.Admin)]
[Route("api/admin/refunds")]
public class AdminRefundsController : BaseApiController
{
    private readonly GetAdminRefundListQuery _getAdminRefundListQuery;
    private readonly ApproveRefundRequest _approveRefundRequest;
    private readonly RejectRefundRequest _rejectRefundRequest;

    public AdminRefundsController(
        GetAdminRefundListQuery getAdminRefundListQuery,
        ApproveRefundRequest approveRefundRequest,
        RejectRefundRequest rejectRefundRequest)
    {
        _getAdminRefundListQuery = getAdminRefundListQuery;
        _approveRefundRequest = approveRefundRequest;
        _rejectRefundRequest = rejectRefundRequest;
    }

    [HttpGet]
    public async Task<IActionResult> GetRefunds(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetAdminRefundListQuery.DefaultPageSize,
        [FromQuery] RefundStatus? status = null)
    {
        return ToActionResult(await _getAdminRefundListQuery.ExecuteAsync(
            page,
            pageSize,
            status));
    }

    [HttpPost("{refundRequestId:guid}/approve")]
    public async Task<IActionResult> Approve(
        Guid refundRequestId,
        [FromBody] ReviewRefundRequestRequest request)
    {
        if (!TryGetUserId(out var adminId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _approveRefundRequest.ExecuteAsync(
            adminId,
            refundRequestId,
            request));
    }

    [HttpPost("{refundRequestId:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid refundRequestId,
        [FromBody] ReviewRefundRequestRequest request)
    {
        if (!TryGetUserId(out var adminId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _rejectRefundRequest.ExecuteAsync(
            adminId,
            refundRequestId,
            request));
    }
}
