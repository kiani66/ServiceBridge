using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.Orders;
using System.Reflection;
using Xunit;

namespace ServiceBridge.Tests.OrderSnapshots;

public sealed class OrderFulfillmentPolicySnapshotTests
{
    [Fact]
    public void Create_WithCompleteValues_SeparatesPolicyAndRuntimeFulfillmentModes()
    {
        var policyId = Guid.NewGuid();
        var snapshot = OrderFulfillmentPolicySnapshot.Create(
            policyId,
            7,
            ProcurementStrategy.PreStockedInventory,
            FulfillmentMode.InventoryCode,
            DeliveryArtifactType.DigitalCode);

        var order = Order.Create(snapshot, FulfillmentMode.ManualAgent);

        Assert.Equal(policyId, order.FulfillmentPolicyId);
        Assert.Equal(7, order.FulfillmentPolicyVersionSnapshot);
        Assert.Equal(ProcurementStrategy.PreStockedInventory, order.ProcurementStrategySnapshot);
        Assert.Equal(FulfillmentMode.InventoryCode, order.PolicyFulfillmentModeSnapshot);
        Assert.Equal(FulfillmentMode.ManualAgent, order.ServiceFulfillmentModeSnapshot);
        Assert.Equal(DeliveryArtifactType.DigitalCode, order.DeliveryArtifactTypeSnapshot);
        Assert.All(
            typeof(OrderFulfillmentPolicySnapshot).GetProperties(),
            property => Assert.Null(property.SetMethod));
    }

    [Fact]
    public void Create_WithEmptyPolicyId_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => Create(policyId: Guid.Empty));
    }

    [Fact]
    public void Order_HasNoPublicSnapshotLessConstructor()
    {
        Assert.Null(typeof(Order).GetConstructor(Type.EmptyTypes));
        Assert.NotNull(typeof(Order).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null));
    }

    [Fact]
    public void Create_WithNonPositiveVersion_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(version: 0));
    }

    [Fact]
    public void OrderCreate_WithUndefinedRuntimeFulfillmentMode_IsRejected()
    {
        var snapshot = Create();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Order.Create(snapshot, (FulfillmentMode)999));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Create_WithUndefinedDecisionValue_IsRejected(int decision)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => decision switch
        {
            1 => Create(procurementStrategy: (ProcurementStrategy)999),
            2 => Create(fulfillmentMode: (FulfillmentMode)999),
            _ => Create(deliveryArtifactType: (DeliveryArtifactType)999)
        });
    }

    private static OrderFulfillmentPolicySnapshot Create(
        Guid? policyId = null,
        int version = 1,
        ProcurementStrategy procurementStrategy = ProcurementStrategy.OnDemandManual,
        FulfillmentMode fulfillmentMode = FulfillmentMode.ManualAgent,
        DeliveryArtifactType deliveryArtifactType = DeliveryArtifactType.AccountAccess)
    {
        return OrderFulfillmentPolicySnapshot.Create(
            policyId ?? Guid.NewGuid(),
            version,
            procurementStrategy,
            fulfillmentMode,
            deliveryArtifactType);
    }
}
