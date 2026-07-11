using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Inventory;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Api.Controllers;

[Authorize(Roles = AppConstants.Roles.Admin)]
[Route("api/admin/inventory")]
public sealed class AdminInventoryController : BaseApiController
{
    private readonly CreateInventoryItemsCommand _createItemsCommand;
    private readonly DisableInventoryItemCommand _disableItemCommand;
    private readonly IAdminInventoryQueryService _queryService;

    public AdminInventoryController(
        CreateInventoryItemsCommand createItemsCommand,
        DisableInventoryItemCommand disableItemCommand,
        IAdminInventoryQueryService queryService)
    {
        _createItemsCommand = createItemsCommand;
        _disableItemCommand = disableItemCommand;
        _queryService = queryService;
    }

    [HttpPost("batch")]
    public async Task<IActionResult> CreateBatch(
        [FromBody] CreateInventoryItemsRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var adminId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _createItemsCommand.ExecuteAsync(
            adminId,
            request,
            cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> GetItems(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = IAdminInventoryQueryService.DefaultPageSize,
        [FromQuery] Guid? serviceVariantId = null,
        [FromQuery] InventoryItemStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        return ToActionResult(await _queryService.GetItemsAsync(
            pageNumber,
            pageSize,
            serviceVariantId,
            status,
            cancellationToken));
    }

    [HttpGet("summary/{serviceVariantId:guid}")]
    public async Task<IActionResult> GetSummary(
        Guid serviceVariantId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await _queryService.GetSummaryAsync(
            serviceVariantId,
            cancellationToken));
    }

    [HttpGet("variants")]
    public async Task<IActionResult> GetVariantOptions(
        [FromQuery] string? search,
        [FromQuery] int pageSize = IAdminInventoryQueryService.DefaultVariantOptionsPageSize,
        CancellationToken cancellationToken = default)
    {
        return ToActionResult(await _queryService.GetVariantOptionsAsync(
            search,
            pageSize,
            cancellationToken));
    }

    [HttpPost("{inventoryItemId:guid}/disable")]
    public async Task<IActionResult> Disable(
        Guid inventoryItemId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var adminId))
        {
            return UnauthorizedServiceResult();
        }

        return ToActionResult(await _disableItemCommand.ExecuteAsync(
            adminId,
            inventoryItemId,
            cancellationToken));
    }
}
