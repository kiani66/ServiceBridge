using ServiceBridge.Application.Audit;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Application.Refunds;

public class ApproveRefundRequest
{
    private readonly IRefundRequestRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogWriter _auditLogWriter;

    public ApproveRefundRequest(
        IRefundRequestRepository repository,
        INotificationService notificationService,
        IAuditLogWriter auditLogWriter)
    {
        _repository = repository;
        _notificationService = notificationService;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<ServiceResult> ExecuteAsync(
        Guid adminId,
        Guid refundRequestId,
        ReviewRefundRequestRequest request)
    {
        var refundRequest = await _repository.GetForReviewAsync(refundRequestId);
        if (refundRequest == null)
        {
            return ServiceResult.Fail(
                AppMessages.RefundRequestNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        try
        {
            refundRequest.Approve(adminId, request.AdminNote, DateTime.UtcNow);
            await _notificationService.QueueRefundApprovedForUserAsync(
                refundRequest.RequestedByUserId,
                refundRequest.OrderId,
                refundRequest.Order.OrderNumber);
        }
        catch (InvalidOperationException)
        {
            return ServiceResult.Fail(
                AppMessages.InvalidRefundTransition,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        if (!await _repository.SaveChangesAsync())
        {
            return ServiceResult.Fail(
                AppMessages.RefundChangedDuringUpdate,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        await _auditLogWriter.WriteAsync(new AuditLogEntry
        {
            ActorId = adminId,
            ActorRole = nameof(UserRole.Admin),
            ActionType = AuditActionType.RefundConfirmed,
            EntityName = nameof(RefundRequest),
            EntityId = refundRequest.Id,
            After = new
            {
                refundRequest.OrderId,
                refundRequest.PaymentId,
                refundRequest.Amount,
                Status = refundRequest.Status.ToString()
            },
            Description = $"Refund request for order {refundRequest.Order.OrderNumber} was approved."
        });

        return ServiceResult.Ok(AppMessages.RefundRequestApproved);
    }
}
