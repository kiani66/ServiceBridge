using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using Xunit;

namespace ServiceBridge.Tests.Fulfillment;

public class FulfillmentPolicyTests
{
    [Fact]
    public void DecisionEnumValues_AreStable()
    {
        Assert.Equal(1, (int)ProcurementStrategy.NotRequired);
        Assert.Equal(2, (int)ProcurementStrategy.PreStockedInventory);
        Assert.Equal(3, (int)ProcurementStrategy.OnDemandManual);
        Assert.Equal(4, (int)ProcurementStrategy.OnDemandSupplierApi);
        Assert.Equal(5, (int)ProcurementStrategy.PreferInventoryThenManual);
        Assert.Equal(
            new[]
            {
                nameof(ProcurementStrategy.NotRequired),
                nameof(ProcurementStrategy.PreStockedInventory),
                nameof(ProcurementStrategy.OnDemandManual),
                nameof(ProcurementStrategy.OnDemandSupplierApi),
                nameof(ProcurementStrategy.PreferInventoryThenManual)
            },
            Enum.GetNames<ProcurementStrategy>());

        Assert.Equal(1, (int)DeliveryArtifactType.DigitalCode);
        Assert.Equal(2, (int)DeliveryArtifactType.GiftLink);
        Assert.Equal(3, (int)DeliveryArtifactType.AccountAccess);
        Assert.Equal(4, (int)DeliveryArtifactType.Instructions);
    }

    [Theory]
    [InlineData(
        ProcurementStrategy.OnDemandManual,
        FulfillmentMode.ManualAgent,
        DeliveryArtifactType.AccountAccess)]
    [InlineData(
        ProcurementStrategy.PreStockedInventory,
        FulfillmentMode.InventoryCode,
        DeliveryArtifactType.DigitalCode)]
    [InlineData(
        ProcurementStrategy.PreferInventoryThenManual,
        FulfillmentMode.InventoryCode,
        DeliveryArtifactType.GiftLink)]
    [InlineData(
        ProcurementStrategy.OnDemandSupplierApi,
        FulfillmentMode.ExternalAutomation,
        DeliveryArtifactType.GiftLink)]
    [InlineData(
        ProcurementStrategy.NotRequired,
        FulfillmentMode.InstructionOnly,
        DeliveryArtifactType.Instructions)]
    public void Create_WithApprovedCombination_Succeeds(
        ProcurementStrategy procurementStrategy,
        FulfillmentMode fulfillmentMode,
        DeliveryArtifactType deliveryArtifactType)
    {
        var serviceVariantId = Guid.NewGuid();

        var policy = FulfillmentPolicy.Create(
            serviceVariantId,
            1,
            procurementStrategy,
            fulfillmentMode,
            deliveryArtifactType);

        Assert.NotEqual(Guid.Empty, policy.Id);
        Assert.Equal(serviceVariantId, policy.ServiceVariantId);
        Assert.Equal(1, policy.Version);
        Assert.Equal(procurementStrategy, policy.ProcurementStrategy);
        Assert.Equal(fulfillmentMode, policy.FulfillmentMode);
        Assert.Equal(deliveryArtifactType, policy.DeliveryArtifactType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveVersion_Throws(int version)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FulfillmentPolicy.Create(
            Guid.NewGuid(),
            version,
            ProcurementStrategy.OnDemandManual,
            FulfillmentMode.ManualAgent,
            DeliveryArtifactType.AccountAccess));
    }

    [Fact]
    public void Create_WithEmptyServiceVariantId_Throws()
    {
        Assert.Throws<ArgumentException>(() => FulfillmentPolicy.Create(
            Guid.Empty,
            1,
            ProcurementStrategy.OnDemandManual,
            FulfillmentMode.ManualAgent,
            DeliveryArtifactType.AccountAccess));
    }

    [Theory]
    [InlineData(typeof(ProcurementStrategy))]
    [InlineData(typeof(FulfillmentMode))]
    [InlineData(typeof(DeliveryArtifactType))]
    public void Create_WithUndefinedDecisionValue_Throws(Type enumType)
    {
        var invalidValue = 999;

        Assert.Throws<ArgumentOutOfRangeException>(() => FulfillmentPolicy.Create(
            Guid.NewGuid(),
            1,
            enumType == typeof(ProcurementStrategy)
                ? (ProcurementStrategy)invalidValue
                : ProcurementStrategy.OnDemandManual,
            enumType == typeof(FulfillmentMode)
                ? (FulfillmentMode)invalidValue
                : FulfillmentMode.ManualAgent,
            enumType == typeof(DeliveryArtifactType)
                ? (DeliveryArtifactType)invalidValue
                : DeliveryArtifactType.AccountAccess));
    }

    [Theory]
    [InlineData(ProcurementStrategy.PreStockedInventory, FulfillmentMode.ManualAgent)]
    [InlineData(ProcurementStrategy.OnDemandSupplierApi, FulfillmentMode.ManualAgent)]
    [InlineData(ProcurementStrategy.OnDemandManual, FulfillmentMode.InventoryCode)]
    [InlineData(ProcurementStrategy.PreStockedInventory, FulfillmentMode.ExternalAutomation)]
    [InlineData(ProcurementStrategy.NotRequired, FulfillmentMode.ExternalAutomation)]
    public void Create_WithIncompatibleProcurementAndFulfillment_Throws(
        ProcurementStrategy procurementStrategy,
        FulfillmentMode fulfillmentMode)
    {
        Assert.Throws<InvalidOperationException>(() => FulfillmentPolicy.Create(
            Guid.NewGuid(),
            1,
            procurementStrategy,
            fulfillmentMode,
            DeliveryArtifactType.DigitalCode));
    }

    [Theory]
    [InlineData(FulfillmentMode.ManualAgent, DeliveryArtifactType.AccountAccess)]
    [InlineData(FulfillmentMode.InventoryCode, DeliveryArtifactType.DigitalCode)]
    [InlineData(FulfillmentMode.ExternalAutomation, DeliveryArtifactType.GiftLink)]
    public void Create_NotRequiredWithNonInstructionMode_Throws(
        FulfillmentMode fulfillmentMode,
        DeliveryArtifactType deliveryArtifactType)
    {
        Assert.Throws<InvalidOperationException>(() => FulfillmentPolicy.Create(
            Guid.NewGuid(),
            1,
            ProcurementStrategy.NotRequired,
            fulfillmentMode,
            deliveryArtifactType));
    }

    [Theory]
    [InlineData(DeliveryArtifactType.DigitalCode)]
    [InlineData(DeliveryArtifactType.GiftLink)]
    [InlineData(DeliveryArtifactType.AccountAccess)]
    public void Create_NotRequiredWithNonInstructionsArtifact_Throws(
        DeliveryArtifactType deliveryArtifactType)
    {
        Assert.Throws<InvalidOperationException>(() => FulfillmentPolicy.Create(
            Guid.NewGuid(),
            1,
            ProcurementStrategy.NotRequired,
            FulfillmentMode.InstructionOnly,
            deliveryArtifactType));
    }

    [Fact]
    public void Create_InstructionOnlyWithProcurement_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => FulfillmentPolicy.Create(
            Guid.NewGuid(),
            1,
            ProcurementStrategy.OnDemandManual,
            FulfillmentMode.InstructionOnly,
            DeliveryArtifactType.Instructions));
    }

    [Fact]
    public void Create_InstructionsArtifactWithNonInstructionMode_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => FulfillmentPolicy.Create(
            Guid.NewGuid(),
            1,
            ProcurementStrategy.OnDemandManual,
            FulfillmentMode.ManualAgent,
            DeliveryArtifactType.Instructions));
    }

    [Theory]
    [InlineData(FulfillmentMode.ManualAgent, DeliveryArtifactType.AccountAccess)]
    [InlineData(FulfillmentMode.ExternalAutomation, DeliveryArtifactType.GiftLink)]
    [InlineData(FulfillmentMode.InstructionOnly, DeliveryArtifactType.Instructions)]
    public void Create_PreferInventoryThenManualWithNonInventoryMode_Throws(
        FulfillmentMode fulfillmentMode,
        DeliveryArtifactType deliveryArtifactType)
    {
        Assert.Throws<InvalidOperationException>(() => FulfillmentPolicy.Create(
            Guid.NewGuid(),
            1,
            ProcurementStrategy.PreferInventoryThenManual,
            fulfillmentMode,
            deliveryArtifactType));
    }

    [Fact]
    public void DecisionFields_HaveNoPublicSetters()
    {
        var immutableProperties = new[]
        {
            nameof(FulfillmentPolicy.ServiceVariantId),
            nameof(FulfillmentPolicy.Version),
            nameof(FulfillmentPolicy.ProcurementStrategy),
            nameof(FulfillmentPolicy.FulfillmentMode),
            nameof(FulfillmentPolicy.DeliveryArtifactType)
        };

        Assert.All(immutableProperties, propertyName =>
        {
            var setter = typeof(FulfillmentPolicy)
                .GetProperty(propertyName)!
                .SetMethod;

            Assert.NotNull(setter);
            Assert.False(setter!.IsPublic);
        });
    }

    [Fact]
    public void Create_DifferentVersionsForSameVariant_AreDistinctPolicies()
    {
        var serviceVariantId = Guid.NewGuid();

        var first = FulfillmentPolicy.Create(
            serviceVariantId,
            1,
            ProcurementStrategy.OnDemandManual,
            FulfillmentMode.ManualAgent,
            DeliveryArtifactType.AccountAccess);
        var second = FulfillmentPolicy.Create(
            serviceVariantId,
            2,
            ProcurementStrategy.OnDemandManual,
            FulfillmentMode.ManualAgent,
            DeliveryArtifactType.AccountAccess);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(first.ServiceVariantId, second.ServiceVariantId);
        Assert.Equal(1, first.Version);
        Assert.Equal(2, second.Version);
    }
}
