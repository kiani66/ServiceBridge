using Microsoft.Extensions.Logging;
using ServiceBridge.Application.Audit;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Inventory;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Infrastructure.Services;

public sealed class AdminInventoryCodeFulfillmentService :
    IAdminInventoryCodeFulfillmentService
{
    private const string RetryCompletedMessage =
        "Inventory code fulfillment retry completed.";
    private const string RetryRejectedMessage =
        "Inventory code fulfillment cannot be retried for this order.";
    private const string PaymentNotCompletedMessage =
        "Inventory code fulfillment requires a completed payment.";
    private const string OrderChangedMessage =
        "The order changed while inventory code fulfillment was retried.";

    private readonly IAdminOrderCommandRepository _orderRepository;
    private readonly FulfillInventoryCodeOrderCommand _fulfillmentCommand;
    private readonly IInventoryCodeFulfillmentTransactionManager _transactionManager;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly INotificationService _notificationService;
    private readonly IRealtimeEventPublisher _realtimeEventPublisher;
    private readonly ILogger<AdminInventoryCodeFulfillmentService> _logger;

    public AdminInventoryCodeFulfillmentService(
        IAdminOrderCommandRepository orderRepository,
        FulfillInventoryCodeOrderCommand fulfillmentCommand,
        IInventoryCodeFulfillmentTransactionManager transactionManager,
        IAuditLogWriter auditLogWriter,
        INotificationService notificationService,
        IRealtimeEventPublisher realtimeEventPublisher,
        ILogger<AdminInventoryCodeFulfillmentService> logger)
    {
        _orderRepository = orderRepository;
        _fulfillmentCommand = fulfillmentCommand;
        _transactionManager = transactionManager;
        _auditLogWriter = auditLogWriter;
        _notificationService = notificationService;
        _realtimeEventPublisher = realtimeEventPublisher;
        _logger = logger;
    }

    public async Task<ServiceResult<RetryInventoryCodeFulfillmentResultDto>>
        RetryAsync(
            Guid adminId,
            Guid orderId,
            CancellationToken cancellationToken = default)
    {
        await using var transaction = await _transactionManager.BeginAsync(
            cancellationToken);
        var order = await _orderRepository.LockOrderForInventoryFulfillmentAsync(
            orderId,
            cancellationToken);
        if (order is null)
        {
            return ServiceResult<RetryInventoryCodeFulfillmentResultDto>.Fail(
                "Order not found.",
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var latestPayment = order.PaymentRecords
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();
        if (latestPayment?.Status != PaymentStatus.Completed)
        {
            return Conflict(PaymentNotCompletedMessage);
        }

        var previousStatus = order.Status;
        var utcNow = DateTime.UtcNow;
        var fulfillment = await _fulfillmentCommand.ExecuteAsync(
            order,
            adminId,
            utcNow,
            cancellationToken);
        if (!fulfillment.Success)
        {
            return Conflict(RetryRejectedMessage);
        }

        var outcome = fulfillment.Data;

        if (outcome is InventoryCodeFulfillmentOutcome.NotInventoryOrder or
            InventoryCodeFulfillmentOutcome.InvalidOrderState or
            InventoryCodeFulfillmentOutcome.InconsistentState)
        {
            return Conflict(RetryRejectedMessage);
        }

        await _auditLogWriter.QueueAsync(
            new AuditLogEntry
            {
                ActorId = adminId,
                ActorRole = nameof(UserRole.Admin),
                ActionType = AuditActionType.InventoryFulfillmentRetried,
                EntityName = nameof(Order),
                EntityId = order.Id,
                Before = new
                {
                    Status = previousStatus.ToString()
                },
                After = new
                {
                    Status = order.Status.ToString(),
                    Outcome = outcome.ToString()
                },
                Description =
                    $"Inventory code fulfillment was retried for order {order.OrderNumber}."
            },
            cancellationToken);

        if (!await _orderRepository.SaveChangesAsync())
        {
            return Conflict(OrderChangedMessage);
        }

        await transaction.CommitAsync(cancellationToken);

        if (outcome == InventoryCodeFulfillmentOutcome.Fulfilled)
        {
            await PublishNotificationRealtimeAsync(order.Id);
        }

        if (outcome is InventoryCodeFulfillmentOutcome.Fulfilled or
            InventoryCodeFulfillmentOutcome.ResumedExistingAllocation)
        {
            await PublishOrderUpdatedAsync(order, utcNow);
        }

        return ServiceResult<RetryInventoryCodeFulfillmentResultDto>.Ok(
            new RetryInventoryCodeFulfillmentResultDto
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Outcome = outcome,
                OrderStatus = order.Status.ToString()
            },
            RetryCompletedMessage);
    }

    private async Task PublishNotificationRealtimeAsync(Guid orderId)
    {
        try
        {
            await _notificationService.PublishQueuedRealtimeAsync();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Inventory fulfillment committed, but notification realtime failed for order {OrderId}.",
                orderId);
        }
    }

    private async Task PublishOrderUpdatedAsync(Order order, DateTime utcNow)
    {
        try
        {
            await _realtimeEventPublisher.PublishOrderUpdatedAsync(
                new OrderUpdatedRealtimeEvent
                {
                    OrderId = order.Id,
                    Status = order.Status.ToString(),
                    LinkUrl = $"/ui/orders/{order.Id}",
                    UpdatedAtUtc = order.UpdatedAt ?? utcNow,
                    UserId = order.UserId,
                    AgentId = order.AssignedAgentId,
                    NotifyAdmins = true
                });
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Inventory fulfillment committed, but order realtime failed for order {OrderId}.",
                order.Id);
        }
    }

    private static ServiceResult<RetryInventoryCodeFulfillmentResultDto> Conflict(
        string message)
    {
        return ServiceResult<RetryInventoryCodeFulfillmentResultDto>.Fail(
            message,
            AppConstants.ErrorCodes.Conflict,
            409);
    }
}
