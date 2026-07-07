using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Application.Refunds;

public class ApproveRefundRequest
{
    private readonly IRefundRequestRepository _repository;
    private readonly INotificationService _notificationService;

    public ApproveRefundRequest(
        IRefundRequestRepository repository,
        INotificationService notificationService)
    {
        _repository = repository;
        _notificationService = notificationService;
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

        return await _repository.SaveChangesAsync()
            ? ServiceResult.Ok(AppMessages.RefundRequestApproved)
            : ServiceResult.Fail(
                AppMessages.RefundChangedDuringUpdate,
                AppConstants.ErrorCodes.Conflict,
                409);
    }
}
