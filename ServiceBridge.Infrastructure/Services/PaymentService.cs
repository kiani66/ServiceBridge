using Microsoft.Extensions.Logging;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Payments;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;
    private readonly IMockPaymentGateway _mockPaymentGateway;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository repository,
        IMockPaymentGateway mockPaymentGateway,
        IPaymentGatewayFactory gatewayFactory,
        INotificationService notificationService,
        ILogger<PaymentService> logger)
    {
        _repository = repository;
        _mockPaymentGateway = mockPaymentGateway;
        _gatewayFactory = gatewayFactory;
        _notificationService = notificationService;
        _logger = logger;
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

        var gatewayResult = _gatewayFactory.GetGateway(request.GatewayProvider);
        if (!gatewayResult.Success || gatewayResult.Data == null)
        {
            return FromFailure<PaymentInitiatedResponse>(gatewayResult);
        }

        var gatewayInitiation = await gatewayResult.Data.InitiateAsync(
            new PaymentGatewayInitiateRequest(
                request.PaymentId,
                order.Id,
                userId,
                order.FinalAmountIrr,
                order.ForeignPrice,
                order.ExchangeRateSnapshot));
        if (!gatewayInitiation.Success || gatewayInitiation.Data == null)
        {
            _logger.LogWarning(
                "Payment gateway initiation failed for payment {PaymentId} through provider {Provider}.",
                request.PaymentId,
                gatewayResult.Data.Provider);

            return FromFailure<PaymentInitiatedResponse>(gatewayInitiation);
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
            new PaymentInitiatedResponse
            {
                PaymentId = payment.Id,
                Provider = gatewayInitiation.Data.Provider,
                GatewayReference = gatewayInitiation.Data.GatewayReference,
                RedirectUrl = gatewayInitiation.Data.RedirectUrl,
                RequiresRedirect = gatewayInitiation.Data.RequiresRedirect,
                ExpiresAtUtc = gatewayInitiation.Data.ExpiresAtUtc
            },
            AppMessages.PaymentInitiated);
    }

    public async Task<ServiceResult<MockPaymentResultResponse>> ProcessMockAsync(
        Guid userId,
        MockPaymentRequest request)
    {
        if (request.PaymentId == Guid.Empty)
        {
            return ValidationFailed<MockPaymentResultResponse>(
                nameof(request.PaymentId),
                "Payment ID is required.");
        }

        var payment = await _repository.GetPaymentWithOrderAsync(request.PaymentId);
        if (payment == null)
        {
            return ServiceResult<MockPaymentResultResponse>.Fail(
                AppMessages.PaymentNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (payment.Order.UserId != userId)
        {
            return ServiceResult<MockPaymentResultResponse>.Fail(
                AppMessages.PaymentOrderForbidden,
                AppConstants.ErrorCodes.Forbidden,
                403);
        }

        var callback = _mockPaymentGateway.CreateCallback(
            request.PaymentId,
            request.Succeed);
        var confirmation = await ConfirmAsync(
            callback.Request,
            callback.RawPayload);
        if (!confirmation.Success)
        {
            return FromFailure<MockPaymentResultResponse>(confirmation);
        }

        return ServiceResult<MockPaymentResultResponse>.Ok(
            new MockPaymentResultResponse
            {
                PaymentId = request.PaymentId,
                OrderId = payment.OrderId,
                Status = callback.Request.Status,
                GatewayReference = callback.Request.GatewayReference
            },
            confirmation.Message);
    }

    public async Task<ServiceResult> ConfirmAsync(
        ConfirmPaymentRequest request,
        string callbackPayload)
    {
        var sanitizedCallbackPayload =
            PaymentGatewayPayloadSanitizer.Sanitize(callbackPayload);

        if (!Enum.IsDefined(request.Status))
        {
            return ValidationFailedCallback(
                nameof(request.Status),
                "Invalid payment status.");
        }

        if (request.GatewayReference?.Length > 200)
        {
            return ValidationFailedCallback(
                nameof(request.GatewayReference),
                "Gateway reference cannot exceed 200 characters.");
        }

        if (request.FailureReason?.Length > 1000)
        {
            return ValidationFailedCallback(
                nameof(request.FailureReason),
                "Failure reason cannot exceed 1000 characters.");
        }

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
                    sanitizedCallbackPayload,
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
            if (IsIdempotentConfirmation(payment.Status, request.Status))
            {
                return ServiceResult.Ok(
                    request.Status == PaymentStatus.Completed
                        ? AppMessages.PaymentConfirmed
                        : AppMessages.PaymentFailed);
            }

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
                    sanitizedCallbackPayload,
                    confirmedAtUtc);
                await _notificationService.QueuePaymentCompletedAsync(
                    payment.Order.UserId,
                    payment.Order.Id,
                    payment.Order.OrderNumber);
                await _notificationService.QueueOrderReadyForAssignmentAsync(
                    payment.Order.Id,
                    payment.Order.OrderNumber);
            }
            else
            {
                payment.Fail(
                    request.GatewayReference,
                    sanitizedCallbackPayload,
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

    public async Task<ServiceResult> VerifyCallbackAsync(
        PaymentGatewayVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        var gatewayResult = _gatewayFactory.GetGateway(request.Provider);
        if (!gatewayResult.Success || gatewayResult.Data == null)
        {
            return ToFailure(gatewayResult);
        }

        var verification = await gatewayResult.Data.VerifyAsync(
            request,
            cancellationToken);
        if (!verification.Success || verification.Data == null)
        {
            _logger.LogWarning(
                "Payment gateway callback verification failed before confirmation for provider {Provider}.",
                gatewayResult.Data.Provider);

            return ToFailure(verification);
        }

        if (verification.Data.PaymentId == null ||
            verification.Data.PaymentId == Guid.Empty)
        {
            return ServiceResult.Fail(
                AppMessages.PaymentGatewayVerificationFailed,
                AppConstants.ErrorCodes.ValidationFailed,
                400);
        }

        var confirmRequest = new ConfirmPaymentRequest
        {
            PaymentId = verification.Data.PaymentId.Value,
            GatewayReference = verification.Data.GatewayReference,
            Status = verification.Data.IsVerified
                ? verification.Data.Status
                : PaymentStatus.Failed,
            FailureReason = verification.Data.IsVerified
                ? verification.Data.FailureReason
                : verification.Data.FailureReason ??
                  AppMessages.PaymentGatewayVerificationFailed
        };

        return await ConfirmAsync(
            confirmRequest,
            verification.Data.SanitizedPayload);
    }

    private static ServiceResult<PaymentInitiatedResponse> ValidationFailed(
        string key,
        string message)
    {
        return ValidationFailed<PaymentInitiatedResponse>(key, message);
    }

    private static ServiceResult<T> ValidationFailed<T>(
        string key,
        string message)
    {
        return new ServiceResult<T>
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

    private static ServiceResult<T> FromFailure<T>(ServiceResult result)
    {
        return new ServiceResult<T>
        {
            Success = false,
            Message = result.Message,
            ErrorCode = result.ErrorCode,
            StatusCode = result.StatusCode,
            ValidationErrors = result.ValidationErrors
        };
    }

    private static ServiceResult ToFailure(ServiceResult result)
    {
        return new ServiceResult
        {
            Success = false,
            Message = result.Message,
            ErrorCode = result.ErrorCode,
            StatusCode = result.StatusCode,
            ValidationErrors = result.ValidationErrors
        };
    }

    private static bool IsIdempotentConfirmation(
        PaymentStatus currentStatus,
        PaymentStatus requestedStatus)
    {
        return currentStatus == requestedStatus &&
               requestedStatus is PaymentStatus.Completed or PaymentStatus.Failed;
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
