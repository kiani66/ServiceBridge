using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Orders;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.User)]
[Route("api/orders")]
public class OrdersController : BaseApiController
{
    private readonly IOrderCreationService _orderCreationService;
    private readonly GetMyOrdersQuery _getMyOrdersQuery;
    private readonly GetMyOrderDetailQuery _getMyOrderDetailQuery;
    private readonly CancelMyOrderCommand _cancelMyOrderCommand;
    private readonly RespondToCustomerActionCommand _respondToCustomerActionCommand;

    public OrdersController(
        IOrderCreationService orderCreationService,
        GetMyOrdersQuery getMyOrdersQuery,
        GetMyOrderDetailQuery getMyOrderDetailQuery,
        CancelMyOrderCommand cancelMyOrderCommand,
        RespondToCustomerActionCommand respondToCustomerActionCommand)
    {
        _orderCreationService = orderCreationService;
        _getMyOrdersQuery = getMyOrdersQuery;
        _getMyOrderDetailQuery = getMyOrderDetailQuery;
        _cancelMyOrderCommand = cancelMyOrderCommand;
        _respondToCustomerActionCommand = respondToCustomerActionCommand;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedResult();
        }

        var result = await _orderCreationService.CreateAsync(userId, request);
        return ToActionResult(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetMyOrdersQuery.DefaultPageSize,
        [FromQuery] OrderStatus? status = null)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedResult();
        }

        var result = await _getMyOrdersQuery.ExecuteAsync(
            userId,
            page,
            pageSize,
            status);
        return ToActionResult(result);
    }

    [HttpGet("my/{orderId:guid}")]
    public async Task<IActionResult> GetMyOrder(Guid orderId)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedResult();
        }

        var result = await _getMyOrderDetailQuery.ExecuteAsync(userId, orderId);
        return ToActionResult(result);
    }

    [HttpPost("my/{orderId:guid}/cancel")]
    public async Task<IActionResult> CancelMyOrder(Guid orderId)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedResult();
        }

        var result = await _cancelMyOrderCommand.ExecuteAsync(userId, orderId);
        return ToActionResult(result);
    }

    [HttpPost("my/{orderId:guid}/customer-actions/respond")]
    public async Task<IActionResult> RespondToCustomerAction(
        Guid orderId,
        [FromBody] RespondToCustomerActionRequest request)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedResult();
        }

        var result = await _respondToCustomerActionCommand.ExecuteAsync(
            userId,
            orderId,
            request);
        return ToActionResult(result);
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }

    private IActionResult UnauthorizedResult()
    {
        return ToActionResult(ServiceResult.Fail(
            AppMessages.UnauthorizedAccess,
            AppConstants.ErrorCodes.Unauthorized,
            401));
    }
}
