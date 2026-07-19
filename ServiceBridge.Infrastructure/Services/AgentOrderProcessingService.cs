using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class AgentOrderProcessingService : IAgentOrderProcessingService
{
    private readonly IAgentOrderCommandRepository _repository;
    private readonly IOrderFieldProtector _fieldProtector;
    private readonly INotificationService _notificationService;
    private readonly IRealtimeEventPublisher _realtimeEventPublisher;
    private readonly IFulfillmentCoordinator _fulfillmentCoordinator;

    public AgentOrderProcessingService(
        IAgentOrderCommandRepository repository,
        IOrderFieldProtector fieldProtector,
        INotificationService notificationService,
        IRealtimeEventPublisher realtimeEventPublisher,
        IFulfillmentCoordinator fulfillmentCoordinator)
    {
        _repository = repository;
        _fieldProtector = fieldProtector;
        _notificationService = notificationService;
        _realtimeEventPublisher = realtimeEventPublisher;
        _fulfillmentCoordinator = fulfillmentCoordinator;
    }

    public Task<ServiceResult> StartProcessingAsync(
        Guid agentId,
        Guid orderId,
        AgentOrderTransitionRequest request)
    {
        return TransitionAsync(
            agentId,
            orderId,
            order => order.StartProcessing(agentId, request.Note),
            order => _notificationService.QueueAgentStartedProcessingAsync(
                order.UserId,
                order.Id,
                order.OrderNumber),
            AppMessages.OrderProcessingStarted,
            coordinate: order =>
                _fulfillmentCoordinator.StartManualAttemptAsync(order));
    }

    public Task<ServiceResult> RequestCustomerActionAsync(
        Guid agentId,
        Guid orderId,
        RequestCustomerActionRequest request)
    {
        return TransitionAsync(
            agentId,
            orderId,
            order => order.RequestCustomerAction(agentId, request.Note),
            order => _notificationService.QueueCustomerActionRequestedAsync(
                order.UserId,
                order.Id,
                order.OrderNumber),
            AppMessages.CustomerActionRequested,
            PublishCustomerActionChangedAsync);
    }

    public Task<ServiceResult> ResumeProcessingAsync(
        Guid agentId,
        Guid orderId,
        AgentOrderTransitionRequest request)
    {
        return TransitionAsync(
            agentId,
            orderId,
            order => order.AcknowledgeCustomerResponse(agentId, request.Note),
            null,
            AppMessages.OrderProcessingResumed);
    }

    public Task<ServiceResult> CompleteAsync(
        Guid agentId,
        Guid orderId,
        AgentOrderTransitionRequest request)
    {
        return CompleteInternalAsync(
            agentId,
            orderId,
            request,
            AppMessages.OrderCompleted);
    }

    public async Task<ServiceResult> SaveDeliveryCredentialsAsync(
        Guid agentId,
        Guid orderId,
        SaveAgentDeliveryCredentialsRequest request)
    {
        var order = await _repository.GetAssignedOrderForUpdateAsync(
            agentId,
            orderId);
        if (order == null)
        {
            return NotFound();
        }

        if (order.Status is not (OrderStatus.Assigned or OrderStatus.Processing))
        {
            return InvalidOperation();
        }

        var definitions = GetAgentFieldDefinitions(order).ToArray();
        var validationErrors = ValidateSubmittedFields(
            definitions,
            request.Credentials);
        if (validationErrors.Count > 0)
        {
            return ServiceResult.ValidationFailed(validationErrors);
        }

        var now = DateTime.UtcNow;
        var existingValues = order.FieldValues
            .Where(x =>
                x.FulfillmentMode == order.FulfillmentMode &&
                x.Actor == CredentialActor.Agent)
            .ToDictionary(x => x.FieldKey, StringComparer.OrdinalIgnoreCase);
        var definitionMap = definitions.ToDictionary(
            x => x.FieldKey,
            StringComparer.OrdinalIgnoreCase);

        foreach (var input in request.Credentials)
        {
            var key = input.FieldKey.Trim();
            var definition = definitionMap[key];
            var protectedValue = _fieldProtector.Protect(input.Value);

            if (existingValues.TryGetValue(key, out var existing))
            {
                existing.EncryptedValue = protectedValue;
                existing.UpdatedAt = now;
                continue;
            }

            order.FieldValues.Add(new OrderFieldValue
            {
                OrderId = order.Id,
                FieldKey = definition.FieldKey,
                FieldLabel = definition.Label,
                FulfillmentMode = definition.FulfillmentMode,
                Actor = definition.Actor,
                EncryptedValue = protectedValue,
                IsSensitive = definition.IsSensitive,
                IsVisibleToCustomerAfterCompletion =
                    definition.IsVisibleToCustomerAfterCompletion,
                DisplayOrder = definition.DisplayOrder,
                CreatedAt = now
            });
        }

        return await SaveAsync(AppMessages.DeliveryCredentialsSaved);
    }

    public Task<ServiceResult> FailAsync(
        Guid agentId,
        Guid orderId,
        FailOrderRequest request)
    {
        return TransitionAsync(
            agentId,
            orderId,
            order => order.Fail(agentId, request.Reason),
            order => _notificationService.QueueOrderFailedAsync(
                order.UserId,
                order.Id,
                order.OrderNumber),
            AppMessages.OrderFailed,
            coordinate: order =>
                _fulfillmentCoordinator.FailManualAttemptAsync(order));
    }

    public async Task<ServiceResult> AddNoteAsync(
        Guid agentId,
        Guid orderId,
        AddAgentOrderNoteRequest request)
    {
        var order = await _repository.GetAssignedOrderForUpdateAsync(
            agentId,
            orderId);
        if (order == null)
        {
            return NotFound();
        }

        try
        {
            order.AddAgentNote(
                agentId,
                request.Text,
                request.IsVisibleToCustomer);
        }
        catch (InvalidOperationException)
        {
            return InvalidOperation();
        }

        return await SaveAsync(AppMessages.OrderNoteAdded);
    }

    private async Task<ServiceResult> TransitionAsync(
        Guid agentId,
        Guid orderId,
        Action<Order> transition,
        Func<Order, Task>? queueNotification,
        string successMessage,
        Func<Order, Task>? publishAfterSave = null,
        Func<Order, Task<ServiceResult>>? coordinate = null)
    {
        var order = await _repository.GetAssignedOrderForUpdateAsync(
            agentId,
            orderId);
        if (order == null)
        {
            return NotFound();
        }

        try
        {
            var previousStatus = order.Status;
            transition(order);
            if (coordinate is not null)
            {
                var coordination = await coordinate(order);
                if (!coordination.Success)
                {
                    return coordination;
                }
            }

            if (queueNotification is not null)
            {
                await queueNotification(order);
            }

            var result = await SaveAsync(successMessage);
            if (result.Success)
            {
                await _notificationService.PublishQueuedRealtimeAsync();

                if (publishAfterSave is not null)
                {
                    await publishAfterSave(order);
                }
                else if (order.Status != previousStatus)
                {
                    await _realtimeEventPublisher.PublishOrderUpdatedAsync(
                        CreateOrderUpdatedEvent(order, notifyAdmins: true));
                }
            }

            return result;
        }
        catch (InvalidOperationException)
        {
            return InvalidOperation();
        }
    }

    private async Task<ServiceResult> CompleteInternalAsync(
        Guid agentId,
        Guid orderId,
        AgentOrderTransitionRequest request,
        string successMessage)
    {
        var order = await _repository.GetAssignedOrderForUpdateAsync(
            agentId,
            orderId);
        if (order == null)
        {
            return NotFound();
        }

        var deliveryErrors = ValidateDeliveryCredentialsBeforeCompletion(order);
        if (deliveryErrors.Count > 0)
        {
            return new ServiceResult
            {
                Success = false,
                Message = AppMessages.RequiredDeliveryCredentialsMissing,
                ErrorCode = AppConstants.ErrorCodes.ValidationFailed,
                StatusCode = 400,
                ValidationErrors = deliveryErrors
            };
        }

        try
        {
            order.Complete(agentId, request.Note);
            var coordination = await _fulfillmentCoordinator
                .CompleteManualAttemptAsync(order);
            if (!coordination.Success)
            {
                return coordination;
            }

            await _notificationService.QueueOrderCompletedAsync(
                order.UserId,
                order.Id,
                order.OrderNumber);
        }
        catch (InvalidOperationException)
        {
            return InvalidOperation();
        }

        var result = await SaveAsync(successMessage);
        if (result.Success)
        {
            await _notificationService.PublishQueuedRealtimeAsync();
            await _realtimeEventPublisher.PublishOrderUpdatedAsync(
                CreateOrderUpdatedEvent(order, notifyAdmins: true));
        }

        return result;
    }

    private static IReadOnlyCollection<OrderFieldDefinition>
        GetAgentFieldDefinitions(Order order)
    {
        return order.Service.RequiredFields
            .Where(x =>
                x.FulfillmentMode == order.FulfillmentMode &&
                x.Actor == CredentialActor.Agent)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new OrderFieldDefinition
            {
                FieldKey = x.FieldKey,
                Label = x.Label,
                FieldType = x.FieldType.ToString(),
                FulfillmentMode = x.FulfillmentMode,
                Actor = x.Actor,
                IsRequired = x.IsRequired,
                IsSensitive = x.IsSensitive,
                IsVisibleToCustomerAfterCompletion =
                    x.IsVisibleToCustomerAfterCompletion,
                DisplayOrder = x.DisplayOrder
            })
            .ToArray();
    }

    private static Dictionary<string, string[]> ValidateSubmittedFields(
        IReadOnlyCollection<OrderFieldDefinition> definitions,
        IReadOnlyCollection<CreateOrderFieldRequest> inputs)
    {
        var errors = new Dictionary<string, List<string>>(
            StringComparer.OrdinalIgnoreCase);
        var knownFields = definitions.ToDictionary(
            x => x.FieldKey,
            StringComparer.OrdinalIgnoreCase);
        var submittedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var input in inputs)
        {
            var key = input.FieldKey?.Trim() ?? string.Empty;
            if (key.Length == 0)
            {
                AddError(errors, nameof(input.FieldKey), "Field key is required.");
                continue;
            }

            if (!submittedFields.Add(key))
            {
                AddError(errors, key, "This field was submitted more than once.");
                continue;
            }

            if (!knownFields.ContainsKey(key))
            {
                AddError(errors, key, "This field is not accepted for delivery credentials.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(input.Value))
            {
                AddError(errors, key, "Field value is required.");
            }
        }

        return ToValidationErrors(errors);
    }

    private static Dictionary<string, string[]> ValidateDeliveryCredentialsBeforeCompletion(
        Order order)
    {
        var errors = new Dictionary<string, List<string>>(
            StringComparer.OrdinalIgnoreCase);
        if (order.FulfillmentMode != OrderFulfillmentMode.CreateNewAccount)
        {
            return ToValidationErrors(errors);
        }

        var requiredFields = GetAgentFieldDefinitions(order)
            .Where(x => x.IsRequired)
            .ToArray();
        var savedFields = order.FieldValues
            .Where(x =>
                x.FulfillmentMode == order.FulfillmentMode &&
                x.Actor == CredentialActor.Agent)
            .ToDictionary(x => x.FieldKey, StringComparer.OrdinalIgnoreCase);

        foreach (var definition in requiredFields)
        {
            if (!savedFields.TryGetValue(definition.FieldKey, out var value) ||
                string.IsNullOrWhiteSpace(value.EncryptedValue))
            {
                AddError(
                    errors,
                    definition.FieldKey,
                    $"{definition.Label} is required before completion.");
            }
        }

        return ToValidationErrors(errors);
    }

    private static void AddError(
        Dictionary<string, List<string>> errors,
        string key,
        string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = new List<string>();
            errors[key] = messages;
        }

        messages.Add(message);
    }

    private static Dictionary<string, string[]> ToValidationErrors(
        Dictionary<string, List<string>> errors)
    {
        return errors.ToDictionary(
            x => x.Key,
            x => x.Value.Distinct().ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<ServiceResult> SaveAsync(string successMessage)
    {
        if (!await _repository.SaveChangesAsync())
        {
            return ServiceResult.Fail(
                AppMessages.OrderChangedDuringAgentProcessing,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        return ServiceResult.Ok(successMessage);
    }

    private static ServiceResult NotFound()
    {
        return ServiceResult.Fail(
            AppMessages.OrderNotFound,
            AppConstants.ErrorCodes.NotFound,
            404);
    }

    private static ServiceResult InvalidOperation()
    {
        return ServiceResult.Fail(
            AppMessages.InvalidAgentOrderOperation,
            AppConstants.ErrorCodes.Conflict,
            409);
    }

    private static OrderUpdatedRealtimeEvent CreateOrderUpdatedEvent(
        Order order,
        bool notifyAdmins)
    {
        return new OrderUpdatedRealtimeEvent
        {
            OrderId = order.Id,
            Status = order.Status.ToString(),
            LinkUrl = $"/ui/orders/{order.Id}",
            UpdatedAtUtc = order.UpdatedAt ?? DateTime.UtcNow,
            UserId = order.UserId,
            AgentId = order.AssignedAgentId,
            NotifyAdmins = notifyAdmins
        };
    }

    private Task PublishCustomerActionChangedAsync(Order order)
    {
        var request = order.CustomerActionRequests
            .Where(x => !x.IsResolved)
            .OrderByDescending(x => x.RequestedAt)
            .FirstOrDefault();

        return request is null
            ? Task.CompletedTask
            : _realtimeEventPublisher.PublishCustomerActionChangedAsync(
                new CustomerActionChangedRealtimeEvent
                {
                    OrderId = order.Id,
                    RequestId = request.Id,
                    LinkUrl = $"/ui/orders/{order.Id}",
                    UpdatedAtUtc = order.UpdatedAt ?? DateTime.UtcNow,
                    UserId = order.UserId,
                    AgentId = order.AssignedAgentId
                });
    }
}
