using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.Agent)]
[Route("api/agent/orders")]
public class AgentOrdersController : BaseApiController
{
    private readonly IAgentOrderQueryService _queryService;
    private readonly IAgentOrderProcessingService _processingService;

    public AgentOrdersController(
        IAgentOrderQueryService queryService,
        IAgentOrderProcessingService processingService)
    {
        _queryService = queryService;
        _processingService = processingService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        return TryGetUserId(out var agentId)
            ? ToActionResult(await _queryService.GetDashboardSummaryAsync(agentId))
            : UnauthorizedServiceResult();
    }

    [HttpGet]
    public async Task<IActionResult> GetAssignedOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = IAgentOrderQueryService.DefaultPageSize,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] string? search = null)
    {
        return TryGetUserId(out var agentId)
            ? ToActionResult(
                await _queryService.GetAssignedOrdersAsync(
                    agentId,
                    page,
                    pageSize,
                    status,
                    search))
            : UnauthorizedServiceResult();
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetAssignedOrder(Guid orderId)
    {
        return TryGetUserId(out var agentId)
            ? ToActionResult(
                await _queryService.GetAssignedOrderDetailAsync(agentId, orderId))
            : UnauthorizedServiceResult();
    }

    [HttpPost("{orderId:guid}/start")]
    public async Task<IActionResult> StartProcessing(
        Guid orderId,
        [FromBody] AgentOrderTransitionRequest request)
    {
        return TryGetUserId(out var agentId)
            ? ToActionResult(
                await _processingService.StartProcessingAsync(
                    agentId,
                    orderId,
                    request))
            : UnauthorizedServiceResult();
    }

    [HttpPost("{orderId:guid}/request-customer-action")]
    public async Task<IActionResult> RequestCustomerAction(
        Guid orderId,
        [FromBody] RequestCustomerActionRequest request)
    {
        return TryGetUserId(out var agentId)
            ? ToActionResult(
                await _processingService.RequestCustomerActionAsync(
                    agentId,
                    orderId,
                    request))
            : UnauthorizedServiceResult();
    }

    [HttpPost("{orderId:guid}/resume")]
    public async Task<IActionResult> ResumeProcessing(
        Guid orderId,
        [FromBody] AgentOrderTransitionRequest request)
    {
        return TryGetUserId(out var agentId)
            ? ToActionResult(
                await _processingService.ResumeProcessingAsync(
                    agentId,
                    orderId,
                    request))
            : UnauthorizedServiceResult();
    }

    [HttpPost("{orderId:guid}/complete")]
    public async Task<IActionResult> Complete(
        Guid orderId,
        [FromBody] AgentOrderTransitionRequest request)
    {
        return TryGetUserId(out var agentId)
            ? ToActionResult(
                await _processingService.CompleteAsync(
                    agentId,
                    orderId,
                    request))
            : UnauthorizedServiceResult();
    }

    [HttpPost("{orderId:guid}/delivery-credentials")]
    public async Task<IActionResult> SaveDeliveryCredentials(
        Guid orderId,
        [FromBody] SaveAgentDeliveryCredentialsRequest request)
    {
        return TryGetUserId(out var agentId)
            ? ToActionResult(
                await _processingService.SaveDeliveryCredentialsAsync(
                    agentId,
                    orderId,
                    request))
            : UnauthorizedServiceResult();
    }

    [HttpPost("{orderId:guid}/fail")]
    public async Task<IActionResult> Fail(
        Guid orderId,
        [FromBody] FailOrderRequest request)
    {
        return TryGetUserId(out var agentId)
            ? ToActionResult(
                await _processingService.FailAsync(
                    agentId,
                    orderId,
                    request))
            : UnauthorizedServiceResult();
    }

    [HttpPost("{orderId:guid}/notes")]
    public async Task<IActionResult> AddNote(
        Guid orderId,
        [FromBody] AddAgentOrderNoteRequest request)
    {
        return TryGetUserId(out var agentId)
            ? ToActionResult(
                await _processingService.AddNoteAsync(agentId, orderId, request))
            : UnauthorizedServiceResult();
    }
}
