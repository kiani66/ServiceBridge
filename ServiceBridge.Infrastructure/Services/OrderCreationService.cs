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
    private readonly IPricingCalculator _pricingCalculator;
    private readonly IOrderFieldValidator _fieldValidator;
    private readonly IOrderNumberGenerator _orderNumberGenerator;
    private readonly IOrderFactory _orderFactory;

    public OrderCreationService(
        IOrderCreationRepository repository,
        IPricingResolver pricingResolver,
        IPricingCalculator pricingCalculator,
        IOrderFieldValidator fieldValidator,
        IOrderNumberGenerator orderNumberGenerator,
        IOrderFactory orderFactory)
    {
        _repository = repository;
        _pricingResolver = pricingResolver;
        _pricingCalculator = pricingCalculator;
        _fieldValidator = fieldValidator;
        _orderNumberGenerator = orderNumberGenerator;
        _orderFactory = orderFactory;
    }

    public async Task<ServiceResult<OrderCreatedResponse>> CreateAsync(
        Guid userId,
        CreateOrderRequest request)
    {
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

        var service = await _repository.GetActiveServiceDefinitionAsync(
            request.ServiceId,
            request.PlanId);
        if (service == null)
        {
            return ServiceResult<OrderCreatedResponse>.Fail(
                request.PlanId.HasValue
                    ? "The selected service variant was not found."
                    : AppMessages.ServiceNotFound,
                AppConstants.ErrorCodes.NotFound,
                404);
        }

        if (service.HasActiveVariants && service.VariantId == null)
        {
            return ValidationFailed(new Dictionary<string, string[]>
            {
                [nameof(request.PlanId)] =
                    ["A service variant must be selected for this service."]
            });
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
            PlanId = service.VariantId,
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

        var pricing = _pricingCalculator.Calculate(
            service.BasePrice,
            resolution.Data.ExchangeRate,
            resolution.Data.MarginPercentage);

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
            Pricing = pricing,
            OrderNumber = orderNumber,
            CreatedAtUtc = now
        });

        await _repository.AddOrderAsync(order);

        return ServiceResult<OrderCreatedResponse>.Created(
            new OrderCreatedResponse
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status.ToString(),
                FulfillmentMode = order.FulfillmentMode,
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
