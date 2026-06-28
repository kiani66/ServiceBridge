using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;

    public PaymentService(IPaymentRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<PaymentInitiatedResponse>> InitiateAsync(
        Guid userId,
        InitiatePaymentRequest request)
    {
        if (request.PaymentId == Guid.Empty)
        {
            return ValidationFailed(nameof(request.PaymentId), "Payment ID is required.");
        }

        var order = await _repository.GetOrderAsync(request.OrderId);
        if (order == null)
        {
            return ServiceResult<PaymentInitiatedResponse>.Fail(
                AppMessages.OrderNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (order.UserId != userId)
        {
            return ServiceResult<PaymentInitiatedResponse>.Fail(
                AppMessages.PaymentOrderForbidden,
                AppConstants.ErrorCodes.Forbidden,
                403);
        }

        if (order.Status != OrderStatus.WaitingForPayment)
        {
            return ServiceResult<PaymentInitiatedResponse>.Fail(
                AppMessages.PaymentOrderNotPending,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        if (await _repository.PaymentExistsAsync(request.PaymentId) ||
            await _repository.PendingPaymentExistsAsync(order.Id))
        {
            return ServiceResult<PaymentInitiatedResponse>.Fail(
                AppMessages.PendingPaymentExists,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var payment = new Payment
        {
            Id = request.PaymentId,
            OrderId = order.Id,
            AmountIRR = order.FinalAmountIrr,
            AmountUSD = order.ForeignPrice,
            ExchangeRateSnapshot = order.ExchangeRateSnapshot,
            CreatedAt = DateTime.UtcNow
        };
        payment.RecordInitiated();

        await _repository.AddPaymentAsync(payment);
        if (!await _repository.SaveChangesAsync())
        {
            return ServiceResult<PaymentInitiatedResponse>.Fail(
                AppMessages.PendingPaymentExists,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        return ServiceResult<PaymentInitiatedResponse>.Created(
            new PaymentInitiatedResponse { PaymentId = payment.Id },
            AppMessages.PaymentInitiated);
    }

    public async Task<ServiceResult> ConfirmAsync(
        ConfirmPaymentRequest request,
        string callbackPayload)
    {
        var payment = await _repository.GetPaymentWithOrderAsync(request.PaymentId);
        if (payment == null)
        {
            return ServiceResult.Fail(
                AppMessages.PaymentNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (request.Status == PaymentStatus.Pending)
        {
            return ValidationFailedCallback(
                nameof(request.Status),
                "A callback cannot keep a payment pending.");
        }

        if (request.Status == PaymentStatus.Refunded)
        {
            try
            {
                payment.Refund(
                    request.GatewayReference,
                    callbackPayload,
                    request.FailureReason,
                    DateTime.UtcNow);
            }
            catch (InvalidOperationException)
            {
                return ServiceResult.Fail(
                    AppMessages.PaymentAlreadyProcessed,
                    AppConstants.ErrorCodes.Conflict,
                    409);
            }

            return await _repository.SaveChangesAsync()
                ? ServiceResult.Ok(AppMessages.PaymentRefunded)
                : ServiceResult.Fail(
                    AppMessages.PaymentAlreadyProcessed,
                    AppConstants.ErrorCodes.Conflict,
                    409);
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            return ServiceResult.Fail(
                AppMessages.PaymentAlreadyProcessed,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var confirmedAtUtc = DateTime.UtcNow;

        try
        {
            if (request.Status == PaymentStatus.Completed)
            {
                payment.Order.MarkPaid(
                    payment.Id,
                    AppMessages.OrderPaidByConfirmedPayment);
                payment.Complete(
                    request.GatewayReference,
                    callbackPayload,
                    confirmedAtUtc);
            }
            else
            {
                payment.Fail(
                    request.GatewayReference,
                    callbackPayload,
                    request.FailureReason ?? "Payment gateway reported failure.",
                    confirmedAtUtc);
            }
        }
        catch (InvalidOperationException)
        {
            return ServiceResult.Fail(
                AppMessages.PaymentOrderNotPending,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        if (!await _repository.SaveChangesAsync())
        {
            return ServiceResult.Fail(
                AppMessages.PaymentAlreadyProcessed,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        return ServiceResult.Ok(
            request.Status == PaymentStatus.Completed
                ? AppMessages.PaymentConfirmed
                : AppMessages.PaymentFailed);
    }

    private static ServiceResult<PaymentInitiatedResponse> ValidationFailed(
        string key,
        string message)
    {
        return new ServiceResult<PaymentInitiatedResponse>
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

    private static ServiceResult ValidationFailedCallback(
        string key,
        string message)
    {
        return ServiceResult.ValidationFailed(
            new Dictionary<string, string[]>
            {
                [key] = [message]
            });
    }
}
