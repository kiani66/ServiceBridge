using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Application.Refunds;

public class CreateRefundRequest
{
    private static readonly HashSet<OrderStatus> EligibleStatuses =
    [
        OrderStatus.Paid,
        OrderStatus.Assigned,
        OrderStatus.Processing,
        OrderStatus.Failed
    ];

    private readonly IRefundRequestRepository _repository;
    private readonly INotificationService _notificationService;

    public CreateRefundRequest(
        IRefundRequestRepository repository,
        INotificationService notificationService)
    {
        _repository = repository;
        _notificationService = notificationService;
    }

    public async Task<ServiceResult<RefundRequestDto>> ExecuteAsync(
        Guid userId,
        Guid orderId,
        CreateRefundRequestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return ValidationFailed(nameof(request.Reason), "Refund reason is required.");
        }

        var order = await _repository.GetOrderWithCompletedPaymentAsync(userId, orderId);
        if (order == null)
        {
            return ServiceResult<RefundRequestDto>.Fail(
                AppMessages.OrderNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (!EligibleStatuses.Contains(order.Status))
        {
            return ServiceResult<RefundRequestDto>.Fail(
                AppMessages.RefundOrderStatusNotEligible,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var payment = OrderPaidPayment(order);
        if (payment == null)
        {
            return ServiceResult<RefundRequestDto>.Fail(
                AppMessages.PaymentNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (await _repository.HasActiveRefundRequestAsync(order.Id, payment.Id))
        {
            return ServiceResult<RefundRequestDto>.Fail(
                AppMessages.ActiveRefundRequestExists,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        RefundRequest refundRequest;
        try
        {
            refundRequest = RefundRequest.Create(
                order.Id,
                payment.Id,
                userId,
                payment.AmountIRR,
                request.Reason,
                DateTime.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return ValidationFailed(nameof(request.Reason), "Refund reason is required.");
        }

        await _repository.AddAsync(refundRequest);
        await _notificationService.QueueRefundRequestedForAdminsAsync(
            order.Id,
            order.OrderNumber);

        if (!await _repository.SaveChangesAsync())
        {
            return ServiceResult<RefundRequestDto>.Fail(
                AppMessages.RefundChangedDuringUpdate,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        return ServiceResult<RefundRequestDto>.Created(
            new RefundRequestDto
            {
                RefundRequestId = refundRequest.Id,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                PaymentId = payment.Id,
                Amount = refundRequest.Amount,
                Reason = refundRequest.Reason,
                Status = refundRequest.Status.ToString(),
                CreatedAtUtc = refundRequest.CreatedAt,
                ReviewedAtUtc = refundRequest.ReviewedAtUtc,
                CompletedAtUtc = refundRequest.CompletedAtUtc,
                AdminNote = refundRequest.AdminNote
            },
            AppMessages.RefundRequestCreated);

        static Payment? OrderPaidPayment(Order value)
        {
            return value.PaymentRecords
                .Where(x => x.Status == PaymentStatus.Completed)
                .OrderByDescending(x => x.ConfirmedAt ?? x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();
        }
    }

    private static ServiceResult<RefundRequestDto> ValidationFailed(
        string key,
        string message)
    {
        return new ServiceResult<RefundRequestDto>
        {
            Success = false,
            Message = AppMessages.ValidationFailed,
            ErrorCode = AppConstants.ErrorCodes.ValidationFailed,
            StatusCode = 400,
            ValidationErrors = new Dictionary<string, string[]>
            {
                [key] = [message]
            }
        };
    }
}
