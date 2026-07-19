using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Services;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.Orders;
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

        var request = ValidRequest(setup);
        request.Fields[1].Value = "secret-password";

        var result = await setup.Service.CreateAsync(userId, request);

        Assert.True(result.Success);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(
            setup.OrderRepository.ServiceDefinition!.ServiceVariantId,
            setup.OrderRepository.RequestedServiceVariantId);
        Assert.Equal(
            setup.OrderRepository.ServiceDefinition.ServiceVariantId,
            result.Data?.ServiceVariantId);
        Assert.Equal(result.Data?.ServiceVariantId, result.Data?.PlanId);

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
        Assert.Equal(FulfillmentMode.ManualAgent, order.ServiceFulfillmentModeSnapshot);
        Assert.Equal(FulfillmentMode.ManualAgent, order.PolicyFulfillmentModeSnapshot);
        Assert.Equal(setup.FulfillmentPolicyResolver.Snapshot!.PolicyId, order.FulfillmentPolicyId);
        Assert.Equal(2, order.FulfillmentPolicyVersionSnapshot);
        Assert.Equal(ProcurementStrategy.OnDemandManual, order.ProcurementStrategySnapshot);
        Assert.Equal(DeliveryArtifactType.AccountAccess, order.DeliveryArtifactTypeSnapshot);
    }

    [Fact]
    public async Task CreateAsync_SnapshotsResolvedFulfillmentPolicy()
    {
        var setup = CreateSetup();
        var policyId = Guid.NewGuid();
        setup.FulfillmentPolicyResolver.Snapshot =
            OrderFulfillmentPolicySnapshot.Create(
                policyId,
                3,
                ProcurementStrategy.PreStockedInventory,
                FulfillmentMode.InventoryCode,
                DeliveryArtifactType.DigitalCode);

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), ValidRequest(setup));

        Assert.True(result.Success);
        var order = Assert.IsType<Domain.Entities.Order>(
            setup.OrderRepository.AddedOrder);
        Assert.Equal(policyId, order.FulfillmentPolicyId);
        Assert.Equal(3, order.FulfillmentPolicyVersionSnapshot);
        Assert.Equal(ProcurementStrategy.PreStockedInventory, order.ProcurementStrategySnapshot);
        Assert.Equal(FulfillmentMode.InventoryCode, order.PolicyFulfillmentModeSnapshot);
        Assert.Equal(FulfillmentMode.ManualAgent, order.ServiceFulfillmentModeSnapshot);
        Assert.Equal(DeliveryArtifactType.DigitalCode, order.DeliveryArtifactTypeSnapshot);
        Assert.Equal(
            setup.OrderRepository.ServiceDefinition!.ServiceVariantId,
            setup.FulfillmentPolicyResolver.RequestedServiceVariantId);

        setup.FulfillmentPolicyResolver.Snapshot = OrderFulfillmentPolicySnapshot.Create(
            Guid.NewGuid(),
            4,
            ProcurementStrategy.OnDemandManual,
            FulfillmentMode.ManualAgent,
            DeliveryArtifactType.AccountAccess);

        Assert.Equal(FulfillmentMode.InventoryCode, order.PolicyFulfillmentModeSnapshot);
        Assert.Equal(FulfillmentMode.ManualAgent, order.ServiceFulfillmentModeSnapshot);
        Assert.Equal(3, order.FulfillmentPolicyVersionSnapshot);
    }

    [Theory]
    [InlineData(
        ProcurementStrategy.PreferInventoryThenManual,
        FulfillmentMode.InventoryCode,
        DeliveryArtifactType.DigitalCode)]
    [InlineData(
        ProcurementStrategy.OnDemandSupplierApi,
        FulfillmentMode.ExternalAutomation,
        DeliveryArtifactType.AccountAccess)]
    [InlineData(
        ProcurementStrategy.NotRequired,
        FulfillmentMode.InstructionOnly,
        DeliveryArtifactType.Instructions)]
    public async Task CreateAsync_FuturePolicyModes_DoNotChangeLegacyRuntimeMode(
        ProcurementStrategy procurementStrategy,
        FulfillmentMode policyFulfillmentMode,
        DeliveryArtifactType deliveryArtifactType)
    {
        var setup = CreateSetup();
        setup.FulfillmentPolicyResolver.Snapshot =
            OrderFulfillmentPolicySnapshot.Create(
                Guid.NewGuid(),
                5,
                procurementStrategy,
                policyFulfillmentMode,
                deliveryArtifactType);

        var result = await setup.Service.CreateAsync(
            Guid.NewGuid(),
            ValidRequest(setup));

        Assert.True(result.Success);
        var order = Assert.IsType<Domain.Entities.Order>(
            setup.OrderRepository.AddedOrder);
        Assert.Equal(policyFulfillmentMode, order.PolicyFulfillmentModeSnapshot);
        Assert.Equal(FulfillmentMode.ManualAgent, order.ServiceFulfillmentModeSnapshot);
    }

    [Fact]
    public async Task CreateAsync_WhenFulfillmentPolicyIsMissing_StopsBeforeOrderAndCredentials()
    {
        var setup = CreateSetup();
        setup.FulfillmentPolicyResolver.Snapshot = null;

        var result = await setup.Service.CreateAsync(
            Guid.NewGuid(),
            ValidRequest(setup));

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.FulfillmentPolicyRequired, result.ErrorCode);
        Assert.Null(setup.OrderRepository.AddedOrder);
        Assert.Equal(0, setup.OrderRepository.SaveCount);
        Assert.Equal(0, setup.OrderFieldProtector.ProtectCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenCheckoutSubmitsCredentials_UsesResolvedPriceAndEncryptsSensitiveFields()
    {
        var setup = CreateSetup();
        setup.OrderRepository.ServiceDefinition!.BasePrice = 50m;
        var request = new CreateOrderRequest
        {
            ServiceId = setup.OrderRepository.ServiceDefinition.ServiceId,
            ServiceVariantId = setup.OrderRepository.ServiceDefinition.ServiceVariantId,
            OrderType = OrderType.Purchase,
            Credentials =
            [
                new CreateOrderFieldRequest
                {
                    FieldKey = "email",
                    Value = "buyer@example.com"
                },
                new CreateOrderFieldRequest
                {
                    FieldKey = "password",
                    Value = "checkout-secret"
                }
            ]
        };

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), request);

        Assert.True(result.Success);
        Assert.Equal(8_250_000m, result.Data?.FinalAmountIrr);
        Assert.Equal(1, setup.PricingRepository.ExchangeRateCallCount);
        var order = Assert.IsType<Domain.Entities.Order>(
            setup.OrderRepository.AddedOrder);
        Assert.Equal(50m, order.ForeignPrice);
        Assert.Equal(8_250_000m, order.FinalAmountIrr);
        Assert.All(
            order.FieldValues,
            field => Assert.StartsWith("protected::", field.EncryptedValue));
        Assert.DoesNotContain(
            order.FieldValues,
            field => field.EncryptedValue == "checkout-secret");
    }

    [Fact]
    public async Task CreateAsync_WhenRequiredFieldIsMissing_ReturnsValidationErrors()
    {
        var setup = CreateSetup();

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), new CreateOrderRequest
        {
            ServiceId = setup.OrderRepository.ServiceDefinition!.ServiceId,
            ServiceVariantId = setup.OrderRepository.ServiceDefinition.ServiceVariantId,
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
    public async Task CreateAsync_WhenCheckoutCredentialsMissRequiredData_FailsBeforePricingOrPersistence()
    {
        var setup = CreateSetup();

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), new CreateOrderRequest
        {
            ServiceId = setup.OrderRepository.ServiceDefinition!.ServiceId,
            ServiceVariantId = setup.OrderRepository.ServiceDefinition.ServiceVariantId,
            OrderType = OrderType.Purchase,
            Credentials =
            [
                new CreateOrderFieldRequest
                {
                    FieldKey = "email",
                    Value = "buyer@example.com"
                },
                new CreateOrderFieldRequest
                {
                    FieldKey = "password",
                    Value = " "
                }
            ]
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.NotNull(result.ValidationErrors);
        Assert.Contains("password", result.ValidationErrors.Keys);
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
            ServiceVariantId = setup.OrderRepository.ServiceDefinition.ServiceVariantId,
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
            ServiceVariantId = setup.OrderRepository.ServiceDefinition.ServiceVariantId,
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
    public async Task CreateAsync_WhenServiceVariantIsMissing_ReturnsValidationErrors()
    {
        var setup = CreateSetup();
        var request = ValidRequest(setup);
        request.ServiceVariantId = null;

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), request);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.NotNull(result.ValidationErrors);
        Assert.Contains(nameof(CreateOrderRequest.ServiceVariantId), result.ValidationErrors.Keys);
        Assert.Contains(nameof(CreateOrderRequest.PlanId), result.ValidationErrors.Keys);
        Assert.Null(setup.OrderRepository.AddedOrder);
        Assert.Equal(0, setup.PricingRepository.ExchangeRateCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenServiceVariantIsSelected_UsesVariantPriceAndSnapshots()
    {
        var setup = CreateSetup();
        var variantId = Guid.NewGuid();
        setup.OrderRepository.ServiceDefinition!.ServiceVariantId = variantId;
        setup.OrderRepository.ServiceDefinition.VariantName = "Monthly / Global";
        setup.OrderRepository.ServiceDefinition.VariantDurationDays = 30;
        setup.OrderRepository.ServiceDefinition.VariantRegionCode = "US";
        setup.OrderRepository.ServiceDefinition.CurrencyCode = "USD";
        setup.OrderRepository.ServiceDefinition.BasePrice = 25m;

        var request = ValidRequest(setup);
        request.ServiceVariantId = variantId;

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), request);

        Assert.True(result.Success);
        Assert.Equal(variantId, result.Data?.ServiceVariantId);
        Assert.Equal(variantId, result.Data?.PlanId);
        Assert.Equal("Monthly / Global", result.Data?.PlanName);
        Assert.Equal(4_125_000m, result.Data?.FinalAmountIrr);

        var order = Assert.IsType<Domain.Entities.Order>(
            setup.OrderRepository.AddedOrder);
        Assert.Equal(setup.OrderRepository.ServiceDefinition.ServiceId, order.ServiceId);
        Assert.Equal(variantId, order.ServiceVariantId);
        Assert.Equal("Monthly / Global", order.VariantNameSnapshot);
        Assert.Equal(30, order.VariantDurationDaysSnapshot);
        Assert.Equal("US", order.VariantRegionCodeSnapshot);
        Assert.Equal(25m, order.ForeignPrice);
        Assert.Equal("USD", order.CurrencyCode);
        Assert.Equal(150_000m, order.ExchangeRateSnapshot);
        Assert.Equal(10m, order.MarginPercentSnapshot);
        Assert.Equal(3_750_000m, order.BaseAmountIrr);
        Assert.Equal(375_000m, order.ProfitAmountIrr);
        Assert.Equal(4_125_000m, order.FinalAmountIrr);
        Assert.Equal("USD", setup.PricingRepository.RequestedCurrencyCode);
        Assert.Equal(1, setup.PricingRepository.VariantMarginCallCount);

        setup.OrderRepository.ServiceDefinition.BasePrice = 50m;
        setup.PricingRepository.ExchangeRate = 200_000m;
        setup.PricingRepository.ServiceMargin = 20m;

        Assert.Equal(25m, order.ForeignPrice);
        Assert.Equal(150_000m, order.ExchangeRateSnapshot);
        Assert.Equal(10m, order.MarginPercentSnapshot);
        Assert.Equal(3_750_000m, order.BaseAmountIrr);
        Assert.Equal(375_000m, order.ProfitAmountIrr);
        Assert.Equal(4_125_000m, order.FinalAmountIrr);
    }

    [Fact]
    public async Task CreateAsync_WhenVariantMarginExists_SnapshotsVariantScopePricing()
    {
        var setup = CreateSetup();
        setup.PricingRepository.VariantMargin = 12m;
        setup.PricingRepository.ServiceMargin = 10m;

        var result = await setup.Service.CreateAsync(
            Guid.NewGuid(),
            ValidRequest(setup));

        Assert.True(result.Success);

        var order = Assert.IsType<Domain.Entities.Order>(
            setup.OrderRepository.AddedOrder);
        Assert.Equal(12m, order.MarginPercentSnapshot);
        Assert.Equal(3_000_000m, order.BaseAmountIrr);
        Assert.Equal(360_000m, order.ProfitAmountIrr);
        Assert.Equal(3_360_000m, order.FinalAmountIrr);
        Assert.Equal(1, setup.PricingRepository.VariantMarginCallCount);
        Assert.Equal(0, setup.PricingRepository.ServiceMarginCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenLegacyPlanIdIsProvided_UsesSameServiceVariantFlow()
    {
        var setup = CreateSetup();
        var variantId = setup.OrderRepository.ServiceDefinition!.ServiceVariantId;
        var request = ValidRequest(setup);
        request.ServiceVariantId = null;
        request.PlanId = variantId;

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), request);

        Assert.True(result.Success);
        Assert.Equal(variantId, setup.OrderRepository.RequestedServiceVariantId);
        Assert.Equal(variantId, result.Data?.ServiceVariantId);
        Assert.Equal(variantId, result.Data?.PlanId);
        var order = Assert.IsType<Domain.Entities.Order>(
            setup.OrderRepository.AddedOrder);
        Assert.Equal(variantId, order.ServiceVariantId);
    }

    [Fact]
    public async Task CreateAsync_WhenOnlyServiceVariantIdIsProvided_CreatesOrder()
    {
        var setup = CreateSetup();
        var request = ValidRequest(setup);
        request.ServiceId = Guid.Empty;

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), request);

        Assert.True(result.Success);
        Assert.Null(setup.OrderRepository.RequestedServiceId);
        Assert.Equal(
            setup.OrderRepository.ServiceDefinition!.ServiceVariantId,
            setup.OrderRepository.RequestedServiceVariantId);
    }

    [Fact]
    public async Task CreateAsync_WhenServiceVariantDoesNotExist_ReturnsNotFound()
    {
        var setup = CreateSetup();
        var request = ValidRequest(setup);
        request.ServiceVariantId = Guid.NewGuid();

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), request);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.NotFound, result.ErrorCode);
        Assert.Null(setup.OrderRepository.AddedOrder);
        Assert.Equal(0, setup.PricingRepository.ExchangeRateCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenServiceVariantIsInactive_ReturnsValidationFailure()
    {
        var setup = CreateSetup();
        setup.OrderRepository.ServiceDefinition!.IsVariantActive = false;

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), ValidRequest(setup));

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.ValidationFailed, result.ErrorCode);
        Assert.Null(setup.OrderRepository.AddedOrder);
        Assert.Equal(0, setup.PricingRepository.ExchangeRateCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenProviderIsInactive_ReturnsNotFound()
    {
        var setup = CreateSetup();
        setup.OrderRepository.ServiceDefinition!.IsProviderActive = false;

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), ValidRequest(setup));

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Null(setup.OrderRepository.AddedOrder);
        Assert.Equal(0, setup.PricingRepository.ExchangeRateCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenParentServiceIsInactive_ReturnsNotFound()
    {
        var setup = CreateSetup();
        setup.OrderRepository.ServiceDefinition!.IsServiceActive = false;

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), ValidRequest(setup));

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Null(setup.OrderRepository.AddedOrder);
        Assert.Equal(0, setup.PricingRepository.ExchangeRateCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenUserIsMissing_StopsBeforeServiceLookup()
    {
        var setup = CreateSetup();
        setup.OrderRepository.UserExists = false;

        var result = await setup.Service.CreateAsync(Guid.NewGuid(), new CreateOrderRequest
        {
            ServiceId = setup.OrderRepository.ServiceDefinition!.ServiceId,
            ServiceVariantId = setup.OrderRepository.ServiceDefinition.ServiceVariantId,
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
            ServiceVariantId = setup.OrderRepository.ServiceDefinition.ServiceVariantId,
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
            ServiceVariantId = setup.OrderRepository.ServiceDefinition.ServiceVariantId,
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
        var serviceVariantId = Guid.NewGuid();
        var orderRepository = new FakeOrderCreationRepository
        {
            ServiceDefinition = new OrderServiceDefinition
            {
                ServiceId = serviceId,
                CategoryId = categoryId,
                ServiceVariantId = serviceVariantId,
                VariantName = "Monthly / Global",
                VariantDurationDays = 30,
                VariantRegionCode = "GLOBAL",
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

        var fulfillmentPolicyResolver = new FakeFulfillmentPolicyResolver
        {
            Snapshot = OrderFulfillmentPolicySnapshot.Create(
                Guid.NewGuid(),
                2,
                ProcurementStrategy.OnDemandManual,
                FulfillmentMode.ManualAgent,
                DeliveryArtifactType.AccountAccess)
        };
        var orderFieldProtector = new FakeOrderFieldProtector();

        var service = new OrderCreationService(
            orderRepository,
            new PricingResolver(pricingRepository, new PricingCalculator()),
            fulfillmentPolicyResolver,
            new OrderFieldValidator(),
            numberGenerator,
            new OrderFactory(orderFieldProtector),
            new FakeNotificationService());

        return new TestSetup(
            service,
            orderRepository,
            pricingRepository,
            fulfillmentPolicyResolver,
            orderFieldProtector);
    }

    private sealed record TestSetup(
        OrderCreationService Service,
        FakeOrderCreationRepository OrderRepository,
        FakePricingRepository PricingRepository,
        FakeFulfillmentPolicyResolver FulfillmentPolicyResolver,
        FakeOrderFieldProtector OrderFieldProtector);
}
