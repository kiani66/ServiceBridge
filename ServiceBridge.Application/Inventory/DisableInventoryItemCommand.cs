using ServiceBridge.Application.Audit;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Application.Inventory;

public sealed class DisableInventoryItemCommand
{
    private readonly IAdminInventoryCommandRepository _repository;
    private readonly IAuditLogWriter _auditLogWriter;

    public DisableInventoryItemCommand(
        IAdminInventoryCommandRepository repository,
        IAuditLogWriter auditLogWriter)
    {
        _repository = repository;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<ServiceResult> ExecuteAsync(
        Guid adminId,
        Guid inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        if (adminId == Guid.Empty)
        {
            return ServiceResult.Fail(
                AppMessages.UnauthorizedAccess,
                AppConstants.ErrorCodes.Unauthorized,
                401);
        }

        if (inventoryItemId == Guid.Empty)
        {
            return ServiceResult.Fail(
                AppMessages.InvalidInventoryDisableRequest,
                AppConstants.ErrorCodes.ValidationFailed,
                400);
        }

        var item = await _repository.GetItemForDisableAsync(
            inventoryItemId,
            cancellationToken);
        if (item is null)
        {
            return ServiceResult.Fail(
                AppMessages.InventoryItemNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (item.Status != InventoryItemStatus.Available)
        {
            return ServiceResult.Fail(
                AppMessages.InventoryItemCannotBeDisabled,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var previousStatus = item.Status;
        try
        {
            item.Disable(adminId, DateTime.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return ServiceResult.Fail(
                AppMessages.InventoryItemCannotBeDisabled,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        try
        {
            await _auditLogWriter.QueueAsync(new AuditLogEntry
            {
                ActorId = adminId,
                ActorRole = nameof(UserRole.Admin),
                ActionType = AuditActionType.InventoryItemDisabled,
                EntityName = nameof(InventoryItem),
                EntityId = item.Id,
                Before = new
                {
                    InventoryItemId = item.Id,
                    item.ServiceVariantId,
                    PreviousStatus = previousStatus.ToString()
                },
                After = new
                {
                    InventoryItemId = item.Id,
                    item.ServiceVariantId,
                    NewStatus = item.Status.ToString()
                },
                Description = "Inventory item disabled."
            }, cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ServiceResult.Fail(
                AppMessages.InventoryOperationUnavailable,
                AppConstants.ErrorCodes.UnexpectedError,
                500);
        }

        var writeResult = await _repository.SaveChangesAsync(cancellationToken);
        if (writeResult == InventoryWriteResult.Conflict)
        {
            return ServiceResult.Fail(
                AppMessages.InventoryItemChangedDuringUpdate,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        if (writeResult != InventoryWriteResult.Succeeded)
        {
            return ServiceResult.Fail(
                AppMessages.InventoryOperationUnavailable,
                AppConstants.ErrorCodes.UnexpectedError,
                500);
        }

        return ServiceResult.Ok(AppMessages.InventoryItemDisabled);
    }
}
