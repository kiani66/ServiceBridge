using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Infrastructure.Services;

public class OrderCreationService : IOrderCreationService
{
    private const int OrderNumberAttempts = 5;

    private readonly IOrderCreationRepository _repository;
    private readonly IPricingResolver _pricingResolver;
    private readonly IOrderFieldValidator _fieldValidator;
    private readonly IOrderNumberGenerator _orderNumberGenerator;
    private readonly IOrderFactory _orderFactory;
    private readonly INotificationService _notificationService;

    public OrderCreationService(
        IOrderCreationRepository repository,
        IPricingResolver pricingResolver,
        IOrderFieldValidator fieldValidator,
        IOrderNumberGenerator orderNumberGenerator,
        IOrderFactory orderFactory,
        INotificationService notificationService)
    {
        _repository = repository;
        _pricingResolver = pricingResolver;
        _fieldValidator = fieldValidator;
        _orderNumberGenerator = orderNumberGenerator;
        _orderFactory = orderFactory;
        _notificationService = notificationService;
    }

    public async Task<ServiceResult<OrderCreatedResponse>> CreateAsync(
        Guid userId,
        CreateOrderRequest request)
    {
        var selectedVariantId = request.ServiceVariantId ?? request.PlanId;
        var selectedVariantValidation = ValidateSelectedVariant(request, selectedVariantId);
        if (selectedVariantValidation != null)
        {
            return selectedVariantValidation;
        }

        if (!Enum.IsDefined(request.OrderType))
        {
            return ValidationFailed(new Dictionary<string, string[]>
            {
                [nameof(request.OrderType)] = ["Invalid order type."]
            });
        }

        if (!Enum.IsDefined(request.FulfillmentMode))
        {
            return ValidationFailed(new Dictionary<string, string[]>
            {
                [nameof(request.FulfillmentMode)] = ["Invalid fulfillment mode."]
            });
        }

        if (!await _repository.UserExistsAsync(userId))
        {
            return ServiceResult<OrderCreatedResponse>.Fail(
                AppMessages.UserNotFound,
                AppConstants.ErrorCodes.UserNotFound,
                404);
        }

        var service = await _repository.GetServiceVariantOrderDefinitionAsync(
            selectedVariantId!.Value,
            request.ServiceId == Guid.Empty ? null : request.ServiceId);
        if (service == null)
        {
            return ServiceResult<OrderCreatedResponse>.Fail(
                "The selected service variant was not found.",
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        var availabilityValidation = ValidateVariantAvailability(request, service);
        if (availabilityValidation != null)
        {
            return availabilityValidation;
        }

        if (!service.SupportsFulfillmentMode(request.FulfillmentMode))
        {
            return ValidationFailed(new Dictionary<string, string[]>
            {
                [nameof(request.FulfillmentMode)] =
                    ["The selected fulfillment mode is not supported for this service."]
            });
        }

        var customerFields = service.RequiredFields
            .Where(x =>
                x.FulfillmentMode == request.FulfillmentMode &&
                x.Actor == CredentialActor.Customer)
            .ToArray();

        var validationErrors = _fieldValidator.Validate(
            customerFields,
            GetCredentialInputs(request));
        if (validationErrors.Count > 0)
        {
            return ValidationFailed(validationErrors);
        }

        var now = DateTime.UtcNow;
        var pricingContext = new ServicePricingContext
        {
            ServiceId = service.ServiceId,
            CategoryId = service.CategoryId,
            ServiceVariantId = service.ServiceVariantId,
            PlanName = service.VariantName,
            PlanDurationDays = service.VariantDurationDays,
            PlanRegionCode = service.VariantRegionCode,
            CurrencyCode = service.CurrencyCode,
            BasePrice = service.BasePrice
        };

        var resolution = await _pricingResolver.ResolveAsync(pricingContext, now);
        if (!resolution.Success || resolution.Data == null)
        {
            return ServiceResult<OrderCreatedResponse>.Fail(
                resolution.Message ?? AppMessages.PricingUnavailable,
                resolution.ErrorCode ?? AppConstants.ErrorCodes.NotFound,
                resolution.StatusCode);
        }

        var orderNumber = await GenerateUniqueOrderNumberAsync(now);
        if (orderNumber == null)
        {
            return ServiceResult<OrderCreatedResponse>.Fail(
                AppMessages.OrderNumberGenerationFailed,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        var order = _orderFactory.Create(new OrderBuildContext
        {
            UserId = userId,
            Request = request,
            Service = service,
            Pricing = new PricingCalculation
            {
                ForeignPrice = resolution.Data.ForeignPrice,
                ExchangeRate = resolution.Data.ExchangeRate,
                MarginPercent = resolution.Data.MarginPercent,
                BaseAmountIrr = resolution.Data.BaseAmountIrr,
                ProfitAmountIrr = resolution.Data.ProfitAmountIrr,
                FinalAmountIrr = resolution.Data.FinalAmountIrr
            },
            OrderNumber = orderNumber,
            CreatedAtUtc = now
        });

        await _repository.AddOrderAsync(order);
        await _notificationService.QueueOrderCreatedAsync(
            order.UserId,
            order.Id,
            order.OrderNumber);
        if (!await _repository.SaveChangesAsync())
        {
            return ServiceResult<OrderCreatedResponse>.Fail(
                AppMessages.OrderNumberGenerationFailed,
                AppConstants.ErrorCodes.Conflict,
                409);
        }

        await _notificationService.PublishQueuedRealtimeAsync();

        return ServiceResult<OrderCreatedResponse>.Created(
            new OrderCreatedResponse
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status.ToString(),
                FulfillmentMode = order.FulfillmentMode,
                ServiceVariantId = order.ServiceVariantId,
                PlanId = order.ServiceVariantId,
                PlanName = order.VariantNameSnapshot,
                PlanDurationDays = order.VariantDurationDaysSnapshot,
                PlanRegionCode = order.VariantRegionCodeSnapshot,
                ForeignPrice = order.ForeignPrice,
                CurrencyCode = order.CurrencyCode,
                ExchangeRate = order.ExchangeRateSnapshot,
                MarginPercentage = order.MarginPercentSnapshot,
                FinalAmountIrr = order.FinalAmountIrr,
                CreatedAt = order.CreatedAt
            },
            AppMessages.OrderCreated);
    }

    internal static IReadOnlyCollection<CreateOrderFieldRequest> GetCredentialInputs(
        CreateOrderRequest request)
    {
        return request.Credentials.Count > 0
            ? request.Credentials
            : request.Fields;
    }

    private static ServiceResult<OrderCreatedResponse>? ValidateSelectedVariant(
        CreateOrderRequest request,
        Guid? selectedVariantId)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.ServiceVariantId.HasValue &&
            request.PlanId.HasValue &&
            request.ServiceVariantId.Value != request.PlanId.Value)
        {
            errors[nameof(request.ServiceVariantId)] =
                ["ServiceVariantId and legacy PlanId must refer to the same variant."];
        }

        if (!selectedVariantId.HasValue || selectedVariantId.Value == Guid.Empty)
        {
            errors[nameof(request.ServiceVariantId)] =
                ["A service variant must be selected."];
            errors[nameof(request.PlanId)] =
                ["A service variant must be selected."];
        }

        return errors.Count == 0 ? null : ValidationFailed(errors);
    }

    private static ServiceResult<OrderCreatedResponse>? ValidateVariantAvailability(
        CreateOrderRequest request,
        OrderServiceDefinition service)
    {
        if (!service.IsVariantActive)
        {
            return ValidationFailed(new Dictionary<string, string[]>
            {
                [nameof(request.ServiceVariantId)] =
                    ["The selected service variant is not active."]
            });
        }

        if (!service.IsServiceActive || !service.IsCategoryActive || !service.IsProviderActive)
        {
            return ServiceResult<OrderCreatedResponse>.Fail(
                AppMessages.ServiceNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        return null;
    }

    private async Task<string?> GenerateUniqueOrderNumberAsync(DateTime now)
    {
        for (var attempt = 0; attempt < OrderNumberAttempts; attempt++)
        {
            var candidate = _orderNumberGenerator.Generate(now);
            if (!await _repository.OrderNumberExistsAsync(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static ServiceResult<OrderCreatedResponse> ValidationFailed(
        Dictionary<string, string[]> errors)
    {
        return new ServiceResult<OrderCreatedResponse>
        {
            Success = false,
            Message = AppMessages.ValidationFailed,
            ErrorCode = AppConstants.ErrorCodes.ValidationFailed,
            StatusCode = 400,
            ValidationErrors = errors
        };
    }
}
