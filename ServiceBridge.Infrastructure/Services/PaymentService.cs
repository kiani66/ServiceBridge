using Microsoft.Extensions.Logging;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Fulfillment;
using ServiceBridge.Application.Inventory;
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
    private readonly IRealtimeEventPublisher _realtimeEventPublisher;
    private readonly FulfillInventoryCodeOrderCommand _fulfillInventoryCodeOrder;
    private readonly IPaymentConfirmationTransactionManager _transactionManager;
    private readonly IFulfillmentCoordinator _fulfillmentCoordinator;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository repository,
        IMockPaymentGateway mockPaymentGateway,
        IPaymentGatewayFactory gatewayFactory,
        INotificationService notificationService,
        IRealtimeEventPublisher realtimeEventPublisher,
        FulfillInventoryCodeOrderCommand fulfillInventoryCodeOrder,
        IPaymentConfirmationTransactionManager transactionManager,
        IFulfillmentCoordinator fulfillmentCoordinator,
        ILogger<PaymentService> logger)
    {
        _repository = repository;
        _mockPaymentGateway = mockPaymentGateway;
        _gatewayFactory = gatewayFactory;
        _notificationService = notificationService;
        _realtimeEventPublisher = realtimeEventPublisher;
        _fulfillInventoryCodeOrder = fulfillInventoryCodeOrder;
        _transactionManager = transactionManager;
        _fulfillmentCoordinator = fulfillmentCoordinator;
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

        var pendingPayment = await _repository.GetPendingPaymentForOrderAsync(order.Id);
        if (pendingPayment is not null)
        {
            return ServiceResult<PaymentInitiatedResponse>.Ok(
                new PaymentInitiatedResponse { PaymentId = pendingPayment.Id },
                AppMessages.PaymentInitiated);
        }

        if (await _repository.PaymentExistsAsync(request.PaymentId))
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

        await using var transaction = await _transactionManager.BeginAsync();

        var payment = request.Status == PaymentStatus.Completed
            ? await _repository.GetPaymentWithOrderForConfirmationAsync(
                request.PaymentId)
            : await _repository.GetPaymentWithOrderAsync(request.PaymentId);
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

            return await SaveCommitAndPublishAsync(
                transaction,
                payment,
                AppMessages.PaymentRefunded,
                publishRealtime: false);
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            if (IsIdempotentConfirmation(payment.Status, request.Status))
            {
                if (request.Status == PaymentStatus.Completed)
                {
                    var initialization = await _fulfillmentCoordinator
                        .EnsureForPaidOrderAsync(payment.Order);
                    if (!initialization.Success)
                    {
                        return ToFailure(initialization);
                    }

                    if (payment.Order.ServiceFulfillmentModeSnapshot ==
                        FulfillmentMode.InventoryCode)
                    {
                        var fulfillment = await FulfillInventoryOrderAsync(
                            payment,
                            DateTime.UtcNow);
                        if (!fulfillment.Success)
                        {
                            return fulfillment.Result;
                        }

                        return await SaveCommitAndPublishAsync(
                            transaction,
                            payment,
                            AppMessages.PaymentConfirmed,
                            fulfillment.OrderChanged);
                    }

                    if (initialization.Data ==
                        FulfillmentInitializationOutcome.Created)
                    {
                        return await SaveCommitAndPublishAsync(
                            transaction,
                            payment,
                            AppMessages.PaymentConfirmed,
                            publishRealtime: false);
                    }
                }

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

                var initialization = await _fulfillmentCoordinator
                    .EnsureForPaidOrderAsync(payment.Order);
                if (!initialization.Success)
                {
                    return ToFailure(initialization);
                }

                await _notificationService.QueuePaymentCompletedAsync(
                    payment.Order.UserId,
                    payment.Order.Id,
                    payment.Order.OrderNumber);

                if (payment.Order.ServiceFulfillmentModeSnapshot ==
                    FulfillmentMode.InventoryCode)
                {
                    var fulfillment = await FulfillInventoryOrderAsync(
                        payment,
                        confirmedAtUtc);
                    if (!fulfillment.Success)
                    {
                        return fulfillment.Result;
                    }
                }
                else
                {
                    await _notificationService.QueueOrderReadyForAssignmentAsync(
                        payment.Order.Id,
                        payment.Order.OrderNumber);
                }
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

        return await SaveCommitAndPublishAsync(
            transaction,
            payment,
            request.Status == PaymentStatus.Completed
                ? AppMessages.PaymentConfirmed
                : AppMessages.PaymentFailed,
            publishRealtime: request.Status == PaymentStatus.Completed);
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

    private async Task<InventoryFulfillmentDecision> FulfillInventoryOrderAsync(
        Payment payment,
        DateTime utcNow)
    {
        var fulfillment = await _fulfillInventoryCodeOrder.ExecuteAsync(
            payment.Order,
            payment.Id,
            utcNow);
        if (!fulfillment.Success)
        {
            return new InventoryFulfillmentDecision(ToFailure(fulfillment), false);
        }

        return fulfillment.Data switch
        {
            InventoryCodeFulfillmentOutcome.Fulfilled =>
                InventoryFulfillmentDecision.Accepted(orderChanged: true),
            InventoryCodeFulfillmentOutcome.ResumedExistingAllocation =>
                InventoryFulfillmentDecision.Accepted(orderChanged: true),
            InventoryCodeFulfillmentOutcome.AlreadyFulfilled =>
                InventoryFulfillmentDecision.Accepted(orderChanged: false),
            InventoryCodeFulfillmentOutcome.NoInventory =>
                InventoryFulfillmentDecision.Accepted(orderChanged: false),
            _ => new InventoryFulfillmentDecision(
                ServiceResult.Fail(
                    AppMessages.PaymentOrderNotPending,
                    AppConstants.ErrorCodes.Conflict,
                    409),
                false)
        };
    }

    private async Task<ServiceResult> SaveCommitAndPublishAsync(
        IPaymentConfirmationTransaction transaction,
        Payment payment,
        string successMessage,
        bool publishRealtime)
    {
        if (!await _repository.SaveChangesAsync())
        {
            return ServiceResult.Fail(
                AppMessages.PaymentAlreadyProcessed,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        await transaction.CommitAsync();

        if (publishRealtime)
        {
            await PublishPostCommitRealtimeAsync(payment);
        }

        return ServiceResult.Ok(successMessage);
    }

    private async Task PublishPostCommitRealtimeAsync(Payment payment)
    {
        try
        {
            await _notificationService.PublishQueuedRealtimeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Payment {PaymentId} committed, but queued notification realtime publication failed for order {OrderId}.",
                payment.Id,
                payment.OrderId);
        }

        await NotifyOrderPaidTransitionAsync(payment);
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

    private static OrderUpdatedRealtimeEvent CreateRealtimeEvent(Order order)
    {
        return new OrderUpdatedRealtimeEvent
        {
            OrderId = order.Id,
            Status = order.Status.ToString(),
            LinkUrl = $"/ui/orders/{order.Id}",
            UpdatedAtUtc = order.UpdatedAt ?? DateTime.UtcNow,
            UserId = order.UserId,
            AgentId = order.AssignedAgentId,
            NotifyAdmins = true
        };
    }

    private async Task NotifyOrderPaidTransitionAsync(Payment payment)
    {
        try
        {
            await _realtimeEventPublisher.PublishOrderUpdatedAsync(
                CreateRealtimeEvent(payment.Order));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Payment {PaymentId} was confirmed, but realtime order update failed for order {OrderId}.",
                payment.Id,
                payment.OrderId);
        }
    }

    private sealed record InventoryFulfillmentDecision(
        ServiceResult Result,
        bool OrderChanged)
    {
        public bool Success => Result.Success;

        public static InventoryFulfillmentDecision Accepted(bool orderChanged)
        {
            return new InventoryFulfillmentDecision(
                ServiceResult.Ok(),
                orderChanged);
        }
    }
}
