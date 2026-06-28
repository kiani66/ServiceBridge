using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Services;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class OrderCreationServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_CreatesEncryptedPricedOrder()
    {
        var setup = CreateSetup();
        var userId = Guid.NewGuid();

        var result = await setup.Service.CreateAsync(userId, new CreateOrderRequest
        {
            ServiceId = setup.OrderRepository.ServiceDefinition!.ServiceId,
            OrderType = OrderType.Purchase,
            Fields =
            [
                new CreateOrderFieldRequest
                {
                    FieldKey = "email",
                    Value = "user@example.com"
                },
                new CreateOrderFieldRequest
                {
                    FieldKey = "password",
                    Value = "secret-password"
                }
            ]
        });

        Assert.True(result.Success);
        Assert.Equal(201, result.StatusCode);

        var order = Assert.IsType<Domain.Entities.Order>(
            setup.OrderRepository.AddedOrder);
        Assert.Equal(OrderStatus.WaitingForPayment, order.Status);
        Assert.Equal(3_300_000m, order.FinalAmountIrr);
        Assert.All(
            order.FieldValues,
            field => Assert.StartsWith("protected::", field.EncryptedValue));
        Assert.DoesNotContain(
            order.FieldValues,
            field => field.EncryptedValue == "secret-password");

        var history = order.StatusHistory.ToArray();
        Assert.Equal(2, history.Length);
        Assert.Null(history[0].PreviousStatus);
        Assert.Equal(OrderStatus.Created, history[0].NewStatus);
        Assert.Equal(OrderStatus.Created, history[1].PreviousStatus);
        Assert.Equal(OrderStatus.WaitingForPayment, history[1].NewStatus);
        Assert.All(history, item => Assert.Equal(userId, item.ChangedByUserId));
    }

    [Fact]
    public async Task CreateAsync_WhenRequiredFieldIsMissing_ReturnsValidationErrors()
    {
        var setup = CreateSetup();

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), new CreateOrderRequest
        {
            ServiceId = setup.OrderRepository.ServiceDefinition!.ServiceId,
            OrderType = OrderType.Purchase
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.ValidationFailed, result.ErrorCode);
        Assert.NotNull(result.ValidationErrors);
        Assert.Null(setup.OrderRepository.AddedOrder);
        Assert.Equal(0, setup.PricingRepository.ExchangeRateCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenFulfillmentModeIsUnsupported_ReturnsValidationErrors()
    {
        var setup = CreateSetup();

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), new CreateOrderRequest
        {
            ServiceId = setup.OrderRepository.ServiceDefinition!.ServiceId,
            OrderType = OrderType.Purchase,
            FulfillmentMode = OrderFulfillmentMode.CreateNewAccount,
            Credentials = []
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.NotNull(result.ValidationErrors);
        Assert.Contains(nameof(CreateOrderRequest.FulfillmentMode), result.ValidationErrors.Keys);
        Assert.Null(setup.OrderRepository.AddedOrder);
    }

    [Fact]
    public async Task CreateAsync_WhenCreateNewAccountIsSupported_CreatesOrderWithSelectedMode()
    {
        var setup = CreateSetup();
        setup.OrderRepository.ServiceDefinition!.SupportsCreateNewAccount = true;
        setup.OrderRepository.ServiceDefinition.RequiredFields =
        [
            ..setup.OrderRepository.ServiceDefinition.RequiredFields,
            new OrderFieldDefinition
            {
                FieldKey = "full_name",
                Label = "Full name",
                FulfillmentMode = OrderFulfillmentMode.CreateNewAccount,
                Actor = CredentialActor.Customer,
                IsRequired = true
            },
            new OrderFieldDefinition
            {
                FieldKey = "created_password",
                Label = "Created password",
                FulfillmentMode = OrderFulfillmentMode.CreateNewAccount,
                Actor = CredentialActor.Agent,
                IsRequired = true,
                IsSensitive = true,
                IsVisibleToCustomerAfterCompletion = true
            }
        ];

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), new CreateOrderRequest
        {
            ServiceId = setup.OrderRepository.ServiceDefinition.ServiceId,
            OrderType = OrderType.Purchase,
            FulfillmentMode = OrderFulfillmentMode.CreateNewAccount,
            Credentials =
            [
                new CreateOrderFieldRequest
                {
                    FieldKey = "full_name",
                    Value = "Test Customer"
                }
            ]
        });

        Assert.True(result.Success);
        var order = Assert.IsType<Domain.Entities.Order>(
            setup.OrderRepository.AddedOrder);
        Assert.Equal(OrderFulfillmentMode.CreateNewAccount, order.FulfillmentMode);
        var field = Assert.Single(order.FieldValues);
        Assert.Equal(CredentialActor.Customer, field.Actor);
        Assert.Equal("full_name", field.FieldKey);
    }

    [Fact]
    public async Task CreateAsync_WhenServiceHasvariantsAndvariantIsMissing_ReturnsValidationErrors()
    {
        var setup = CreateSetup();
        setup.OrderRepository.ServiceDefinition!.HasActiveVariants = true;

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), ValidRequest(setup));

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.NotNull(result.ValidationErrors);
        Assert.Contains(nameof(CreateOrderRequest.PlanId), result.ValidationErrors.Keys);
        Assert.Null(setup.OrderRepository.AddedOrder);
        Assert.Equal(0, setup.PricingRepository.ExchangeRateCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenvariantIsSelected_UsesvariantPriceAndSnapshots()
    {
        var setup = CreateSetup();
        var variantId = Guid.NewGuid();
        setup.OrderRepository.ServiceDefinition!.HasActiveVariants = true;
        setup.OrderRepository.ServiceDefinition.VariantId = variantId;
        setup.OrderRepository.ServiceDefinition.VariantName = "Monthly / Global";
        setup.OrderRepository.ServiceDefinition.VariantDurationDays = 30;
        setup.OrderRepository.ServiceDefinition.VariantRegionCode = "GLOBAL";
        setup.OrderRepository.ServiceDefinition.BasePrice = 25m;

        var request = ValidRequest(setup);
        request.PlanId = variantId;

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), request);

        Assert.True(result.Success);
        Assert.Equal(variantId, result.Data?.PlanId);
        Assert.Equal("Monthly / Global", result.Data?.PlanName);
        Assert.Equal(4_125_000m, result.Data?.FinalAmountIrr);

        var order = Assert.IsType<Domain.Entities.Order>(
            setup.OrderRepository.AddedOrder);
        Assert.Equal(variantId, order.ServiceVariantId);
        Assert.Equal("Monthly / Global", order.VariantNameSnapshot);
        Assert.Equal(30, order.VariantDurationDaysSnapshot);
        Assert.Equal("GLOBAL", order.VariantRegionCodeSnapshot);
        Assert.Equal(25m, order.ForeignPrice);
    }

    [Fact]
    public async Task CreateAsync_WhenUserIsMissing_StopsBeforeServiceLookup()
    {
        var setup = CreateSetup();
        setup.OrderRepository.UserExists = false;

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), new CreateOrderRequest
        {
            ServiceId = setup.OrderRepository.ServiceDefinition!.ServiceId,
            OrderType = OrderType.Purchase
        });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Null(setup.OrderRepository.AddedOrder);
    }

    [Fact]
    public async Task CreateAsync_WhenOrderTypeIsInvalid_ReturnsValidationFailure()
    {
        var setup = CreateSetup();

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), new CreateOrderRequest
        {
            ServiceId = setup.OrderRepository.ServiceDefinition!.ServiceId,
            OrderType = (OrderType)999,
            Fields = []
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(0, setup.PricingRepository.ExchangeRateCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenOrderNumbersCollide_ReturnsConflict()
    {
        var setup = CreateSetup(
            "PB-DUPLICATE",
            "PB-DUPLICATE",
            "PB-DUPLICATE",
            "PB-DUPLICATE",
            "PB-DUPLICATE");
        setup.OrderRepository.ExistingOrderNumbers.Add("PB-DUPLICATE");

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), ValidRequest(setup));

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Null(setup.OrderRepository.AddedOrder);
    }

    private static CreateOrderRequest ValidRequest(TestSetup setup)
    {
        return new CreateOrderRequest
        {
            ServiceId = setup.OrderRepository.ServiceDefinition!.ServiceId,
            OrderType = OrderType.Purchase,
            Fields =
            [
                new CreateOrderFieldRequest
                {
                    FieldKey = "email",
                    Value = "user@example.com"
                },
                new CreateOrderFieldRequest
                {
                    FieldKey = "password",
                    Value = "secret"
                }
            ]
        };
    }

    private static TestSetup CreateSetup(params string[] orderNumbers)
    {
        var serviceId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var orderRepository = new FakeOrderCreationRepository
        {
            ServiceDefinition = new OrderServiceDefinition
            {
                ServiceId = serviceId,
                CategoryId = categoryId,
                CurrencyCode = "USD",
                BasePrice = 20m,
                RequiredFields =
                [
                    new OrderFieldDefinition
                    {
                        FieldKey = "email",
                        Label = "Email",
                        IsRequired = true
                    },
                    new OrderFieldDefinition
                    {
                        FieldKey = "password",
                        Label = "Password",
                        IsRequired = true,
                        IsSensitive = true
                    }
                ]
            }
        };

        var pricingRepository = new FakePricingRepository
        {
            ServiceContext = new ServicePricingContext
            {
                ServiceId = serviceId,
                CategoryId = categoryId,
                CurrencyCode = "USD",
                BasePrice = 20m
            },
            ExchangeRate = 150_000m,
            ServiceMargin = 10m
        };

        var numberGenerator = orderNumbers.Length == 0
            ? new FakeOrderNumberGenerator("PB-20260621-ABC12345")
            : new FakeOrderNumberGenerator(orderNumbers);

        var service = new OrderCreationService(
            orderRepository,
            new PricingResolver(pricingRepository),
            new PricingCalculator(),
            new OrderFieldValidator(),
            numberGenerator,
            new OrderFactory(new FakeOrderFieldProtector()));

        return new TestSetup(service, orderRepository, pricingRepository);
    }

    private sealed record TestSetup(
        OrderCreationService Service,
        FakeOrderCreationRepository OrderRepository,
        FakePricingRepository PricingRepository);
}
