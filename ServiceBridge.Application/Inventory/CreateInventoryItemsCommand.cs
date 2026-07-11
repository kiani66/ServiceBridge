using ServiceBridge.Application.Audit;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Application.Inventory;

public sealed class CreateInventoryItemsCommand
{
    private const int MaximumBatchSize = 100;
    private const int MaximumCodeLength = 1000;

    private readonly IAdminInventoryCommandRepository _repository;
    private readonly IInventoryCodeProtector _codeProtector;
    private readonly IAuditLogWriter _auditLogWriter;

    public CreateInventoryItemsCommand(
        IAdminInventoryCommandRepository repository,
        IInventoryCodeProtector codeProtector,
        IAuditLogWriter auditLogWriter)
    {
        _repository = repository;
        _codeProtector = codeProtector;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<ServiceResult<CreateInventoryItemsResponse>> ExecuteAsync(
        Guid adminId,
        CreateInventoryItemsRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (adminId == Guid.Empty)
        {
            return ServiceResult<CreateInventoryItemsResponse>.Fail(
                AppMessages.UnauthorizedAccess,
                AppConstants.ErrorCodes.Unauthorized,
                401);
        }

        if (request is null || request.ServiceVariantId == Guid.Empty ||
            request.Codes is not { Count: >= 1 and <= MaximumBatchSize })
        {
            return InvalidRequest();
        }

        if (request.Codes.Any(code =>
                string.IsNullOrWhiteSpace(code) || code.Length > MaximumCodeLength))
        {
            return InvalidRequest();
        }

        var target = await _repository.GetServiceVariantAsync(
            request.ServiceVariantId,
            cancellationToken);
        if (target is null)
        {
            return ServiceResult<CreateInventoryItemsResponse>.Fail(
                AppMessages.ServiceVariantNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (!target.IsActive)
        {
            return ServiceResult<CreateInventoryItemsResponse>.Fail(
                AppMessages.InventoryVariantInactive,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        if (target.FulfillmentMode != FulfillmentMode.InventoryCode)
        {
            return ServiceResult<CreateInventoryItemsResponse>.Fail(
                AppMessages.InventoryRequiresInventoryCodeFulfillment,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        List<(string Code, string Fingerprint)> fingerprintedCodes;
        try
        {
            fingerprintedCodes = request.Codes
                .Select(code => (
                    Code: code,
                    Fingerprint: _codeProtector.ComputeFingerprint(code.Trim())))
                .ToList();
        }
        catch (InvalidOperationException)
        {
            return ServiceResult<CreateInventoryItemsResponse>.Fail(
                AppMessages.InventoryProtectionUnavailable,
                AppConstants.ErrorCodes.UnexpectedError,
                503);
        }

        var fingerprints = fingerprintedCodes.Select(x => x.Fingerprint).ToArray();
        if (fingerprints.Distinct(StringComparer.Ordinal).Count() != fingerprints.Length ||
            await _repository.AnyFingerprintsAsync(fingerprints, cancellationToken))
        {
            return ServiceResult<CreateInventoryItemsResponse>.Fail(
                AppMessages.InventoryDuplicateValue,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        List<(string ProtectedValue, string Fingerprint)> securedCodes;
        try
        {
            securedCodes = fingerprintedCodes
                .Select(code => (
                    ProtectedValue: _codeProtector.Protect(code.Code),
                    code.Fingerprint))
                .ToList();
        }
        catch (InvalidOperationException)
        {
            return ServiceResult<CreateInventoryItemsResponse>.Fail(
                AppMessages.InventoryProtectionUnavailable,
                AppConstants.ErrorCodes.UnexpectedError,
                503);
        }

        var createdAtUtc = DateTime.UtcNow;
        var items = securedCodes
            .Select(x => InventoryItem.Create(
                target.ServiceVariantId,
                target.FulfillmentMode,
                x.ProtectedValue,
                x.Fingerprint,
                adminId,
                createdAtUtc))
            .ToArray();

        await _repository.StageItemsAsync(items, cancellationToken);

        try
        {
            foreach (var item in items)
            {
                await _auditLogWriter.QueueAsync(new AuditLogEntry
                {
                    ActorId = adminId,
                    ActorRole = nameof(UserRole.Admin),
                    ActionType = AuditActionType.InventoryItemCreated,
                    EntityName = nameof(InventoryItem),
                    EntityId = item.Id,
                    After = new
                    {
                        item.ServiceVariantId,
                        Status = item.Status.ToString(),
                        BatchSize = items.Length
                    },
                    Description = "Inventory item created."
                }, cancellationToken);
            }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return ServiceResult<CreateInventoryItemsResponse>.Fail(
                AppMessages.InventoryOperationUnavailable,
                AppConstants.ErrorCodes.UnexpectedError,
                500);
        }

        var writeResult = await _repository.SaveChangesAsync(cancellationToken);
        if (writeResult == InventoryWriteResult.Conflict)
        {
            return ServiceResult<CreateInventoryItemsResponse>.Fail(
                AppMessages.InventoryDuplicateValue,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        if (writeResult != InventoryWriteResult.Succeeded)
        {
            return ServiceResult<CreateInventoryItemsResponse>.Fail(
                AppMessages.InventoryOperationUnavailable,
                AppConstants.ErrorCodes.UnexpectedError,
                500);
        }

        return ServiceResult<CreateInventoryItemsResponse>.Created(
            new CreateInventoryItemsResponse
            {
                ServiceVariantId = target.ServiceVariantId,
                ServiceName = target.ServiceName,
                VariantName = target.VariantName,
                CreatedCount = items.Length
            },
            AppMessages.InventoryItemsCreated);
    }

    private static ServiceResult<CreateInventoryItemsResponse> InvalidRequest()
    {
        return ServiceResult<CreateInventoryItemsResponse>.Fail(
            AppMessages.InvalidInventoryImportRequest,
            AppConstants.ErrorCodes.ValidationFailed,
            400);
    }
}
