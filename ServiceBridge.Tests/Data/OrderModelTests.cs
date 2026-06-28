using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;
using Xunit;

namespace ServiceBridge.Tests.Data;

public class OrderModelTests
{
    private readonly ServiceBridgeDbContext _context;

    public OrderModelTests()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ServiceBridgeOrderModelTests")
            .Options;

        _context = new ServiceBridgeDbContext(options);
    }

    [Fact]
    public void OrderEntities_UseClientGeneratedIds()
    {
        AssertClientGeneratedId<Order>();
        AssertClientGeneratedId<OrderFieldValue>();
        AssertClientGeneratedId<OrderStatusHistory>();
        AssertClientGeneratedId<OrderNote>();
        AssertClientGeneratedId<Payment>();
    }

    [Fact]
    public void OrderNumber_IsUnique()
    {
        var orderType = _context.Model.FindEntityType(typeof(Order))!;

        Assert.Contains(
            orderType.GetIndexes(),
            index => index.IsUnique &&
                     index.Properties.Select(x => x.Name)
                         .SequenceEqual(new[] { nameof(Order.OrderNumber) }));
    }

    [Fact]
    public void OrderPricingSnapshot_UsesExpectedPrecision()
    {
        var orderType = _context.Model.FindEntityType(typeof(Order))!;

        AssertPrecision(orderType, nameof(Order.ForeignPrice), 18, 4);
        AssertPrecision(orderType, nameof(Order.ExchangeRateSnapshot), 18, 4);
        AssertPrecision(orderType, nameof(Order.MarginPercentSnapshot), 5, 2);
        AssertPrecision(orderType, nameof(Order.BaseAmountIrr), 18, 0);
        AssertPrecision(orderType, nameof(Order.ProfitAmountIrr), 18, 0);
        AssertPrecision(orderType, nameof(Order.FinalAmountIrr), 18, 0);
    }

    [Fact]
    public void Order_UsesServiceVariantForeignKey()
    {
        var orderType = _context.Model.FindEntityType(typeof(Order))!;

        Assert.NotNull(orderType.FindProperty(nameof(Order.ServiceVariantId)));
        Assert.Null(orderType.FindProperty("ServicePlanId"));
        Assert.Contains(
            orderType.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(ServiceVariant) &&
                          foreignKey.Properties.Select(x => x.Name)
                              .SequenceEqual(new[] { nameof(Order.ServiceVariantId) }));
    }

    [Fact]
    public void Order_StoresVariantSnapshots()
    {
        var orderType = _context.Model.FindEntityType(typeof(Order))!;

        Assert.NotNull(orderType.FindProperty(nameof(Order.VariantNameSnapshot)));
        Assert.NotNull(orderType.FindProperty(nameof(Order.VariantDurationDaysSnapshot)));
        Assert.NotNull(orderType.FindProperty(nameof(Order.VariantRegionCodeSnapshot)));
        Assert.Null(orderType.FindProperty("PlanNameSnapshot"));
        Assert.Null(orderType.FindProperty("PlanDurationDaysSnapshot"));
        Assert.Null(orderType.FindProperty("PlanRegionCodeSnapshot"));
    }

    [Fact]
    public void Order_HasPricingIntegrityConstraints()
    {
        var designTimeModel = _context.GetService<IDesignTimeModel>().Model;
        var orderType = designTimeModel.FindEntityType(typeof(Order))!;
        var constraintNames = orderType
            .GetCheckConstraints()
            .Select(x => x.Name)
            .ToArray();

        Assert.Contains("CK_Orders_Pricing_NonNegative", constraintNames);
        Assert.Contains("CK_Orders_FinalAmount_Matches", constraintNames);
    }

    [Fact]
    public void Order_RowVersionIsConcurrencyToken()
    {
        var orderType = _context.Model.FindEntityType(typeof(Order))!;
        var rowVersion = orderType.FindProperty(nameof(Order.RowVersion))!;

        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
    }

    [Fact]
    public void OrderStatusHistory_CannotBeModified()
    {
        var history = new OrderStatusHistory
        {
            OrderId = Guid.NewGuid(),
            NewStatus = Domain.Enums.OrderStatus.WaitingForPayment,
            ChangedByUserId = Guid.NewGuid()
        };

        _context.OrderStatusHistory.Attach(history);
        history.Note = "Changed after creation";

        var exception = Assert.Throws<InvalidOperationException>(
            () => _context.SaveChanges());

        Assert.Equal("Order status history is append-only.", exception.Message);
    }

    [Fact]
    public void OrderStatusHistory_CannotBeDeleted()
    {
        var history = new OrderStatusHistory
        {
            OrderId = Guid.NewGuid(),
            NewStatus = Domain.Enums.OrderStatus.WaitingForPayment,
            ChangedByUserId = Guid.NewGuid()
        };

        _context.OrderStatusHistory.Attach(history);
        _context.OrderStatusHistory.Remove(history);

        var exception = Assert.Throws<InvalidOperationException>(
            () => _context.SaveChanges());

        Assert.Equal("Order status history is append-only.", exception.Message);
    }

    [Fact]
    public void OrderStatusAndPricingSnapshots_AreNotPubliclyMutable()
    {
        var protectedProperties = new[]
        {
            nameof(Order.Status),
            nameof(Order.ForeignPrice),
            nameof(Order.CurrencyCode),
            nameof(Order.ExchangeRateSnapshot),
            nameof(Order.MarginPercentSnapshot),
            nameof(Order.BaseAmountIrr),
            nameof(Order.ProfitAmountIrr),
            nameof(Order.FinalAmountIrr),
            nameof(Order.VariantNameSnapshot),
            nameof(Order.VariantDurationDaysSnapshot),
            nameof(Order.VariantRegionCodeSnapshot)
        };

        foreach (var propertyName in protectedProperties)
        {
            var setter = typeof(Order).GetProperty(propertyName)!.SetMethod!;
            Assert.False(setter.IsPublic);
        }
    }

    [Fact]
    public void Payment_UsesExpectedPrecisionAndPendingUniqueness()
    {
        var paymentType = _context.Model.FindEntityType(typeof(Payment))!;

        AssertPrecision(paymentType, nameof(Payment.AmountIRR), 18, 0);
        AssertPrecision(paymentType, nameof(Payment.AmountUSD), 18, 4);
        AssertPrecision(paymentType, nameof(Payment.ExchangeRateSnapshot), 18, 4);

        Assert.Contains(
            paymentType.GetIndexes(),
            index => index.IsUnique &&
                     index.GetFilter() == "[Status] = 1" &&
                     index.Properties.Select(x => x.Name)
                         .SequenceEqual(new[] { nameof(Payment.OrderId) }));
        Assert.Contains(
            paymentType.GetIndexes(),
            index => index.IsUnique &&
                     index.GetFilter() == "[Status] = 2" &&
                     index.Properties.Select(x => x.Name)
                         .SequenceEqual(new[] { nameof(Payment.OrderId) }));
    }

    [Fact]
    public void PaymentAndOrderLog_HaveIntegrityConstraints()
    {
        var designTimeModel = _context.GetService<IDesignTimeModel>().Model;
        var paymentType = designTimeModel.FindEntityType(typeof(Payment))!;
        var historyType = designTimeModel.FindEntityType(typeof(OrderStatusHistory))!;

        Assert.Contains(
            paymentType.GetCheckConstraints(),
            constraint => constraint.Name == "CK_Payments_Amounts_Positive");
        Assert.Contains(
            paymentType.GetCheckConstraints(),
            constraint => constraint.Name == "CK_Payments_Status_Valid");
        Assert.Contains(
            historyType.GetCheckConstraints(),
            constraint => constraint.Name == "CK_OrderStatusHistory_ActorType_Valid");
    }

    [Fact]
    public void OrderChildren_CascadeWhileExternalReferencesAreRestricted()
    {
        var orderType = _context.Model.FindEntityType(typeof(Order))!;
        var fieldType = _context.Model.FindEntityType(typeof(OrderFieldValue))!;
        var historyType = _context.Model.FindEntityType(typeof(OrderStatusHistory))!;
        var noteType = _context.Model.FindEntityType(typeof(OrderNote))!;

        Assert.All(
            orderType.GetForeignKeys(),
            foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));

        Assert.Contains(
            fieldType.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Order) &&
                          foreignKey.DeleteBehavior == DeleteBehavior.Cascade);

        Assert.Contains(
            historyType.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Order) &&
                          foreignKey.DeleteBehavior == DeleteBehavior.Cascade);

        Assert.Contains(
            noteType.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Order) &&
                          foreignKey.DeleteBehavior == DeleteBehavior.Cascade);
    }

    [Fact]
    public void OrderFieldValue_StoresEncryptedValueOnly()
    {
        var fieldType = _context.Model.FindEntityType(typeof(OrderFieldValue))!;

        Assert.NotNull(fieldType.FindProperty(nameof(OrderFieldValue.EncryptedValue)));
        Assert.Null(fieldType.FindProperty("Value"));
        Assert.Null(fieldType.FindProperty("PlainTextValue"));
    }

    private void AssertClientGeneratedId<TEntity>()
    {
        var entityType = _context.Model.FindEntityType(typeof(TEntity))!;
        var idProperty = entityType.FindProperty(nameof(BaseEntity.Id))!;

        Assert.Equal(ValueGenerated.Never, idProperty.ValueGenerated);
    }

    private static void AssertPrecision(
        IEntityType entityType,
        string propertyName,
        int precision,
        int scale)
    {
        var property = entityType.FindProperty(propertyName)!;

        Assert.Equal(precision, property.GetPrecision());
        Assert.Equal(scale, property.GetScale());
    }
}
