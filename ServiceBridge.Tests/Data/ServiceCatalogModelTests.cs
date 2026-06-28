using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Data;
using Xunit;

namespace ServiceBridge.Tests.Data;

public class ServiceCatalogModelTests
{
    private readonly ServiceBridgeDbContext _context;

    public ServiceCatalogModelTests()
    {
        var options = new DbContextOptionsBuilder<ServiceBridgeDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ServiceBridgeModelTests")
            .Options;

        _context = new ServiceBridgeDbContext(options);
    }

    [Fact]
    public void CatalogEntities_UseClientGeneratedIds()
    {
        AssertClientGeneratedId<ServiceCategory>();
        AssertClientGeneratedId<DigitalService>();
        AssertClientGeneratedId<ServicePlan>();
        AssertClientGeneratedId<ServiceRequiredField>();
    }

    [Fact]
    public void CategoryAndServiceSlugs_AreUnique()
    {
        var categoryType = _context.Model.FindEntityType(typeof(ServiceCategory))!;
        var serviceType = _context.Model.FindEntityType(typeof(DigitalService))!;

        Assert.Contains(
            categoryType.GetIndexes(),
            index => index.IsUnique &&
                     index.Properties.Select(x => x.Name).SequenceEqual(new[] { "Slug" }));

        Assert.Contains(
            serviceType.GetIndexes(),
            index => index.IsUnique &&
                     index.Properties.Select(x => x.Name).SequenceEqual(new[] { "Slug" }));
    }

    [Fact]
    public void ServiceBasePrice_UsesExpectedPrecision()
    {
        var serviceType = _context.Model.FindEntityType(typeof(DigitalService))!;
        var basePrice = serviceType.FindProperty(nameof(DigitalService.BasePrice))!;

        Assert.Equal(18, basePrice.GetPrecision());
        Assert.Equal(4, basePrice.GetScale());

        var planType = _context.Model.FindEntityType(typeof(ServicePlan))!;
        var planBasePrice = planType.FindProperty(nameof(ServicePlan.BasePrice))!;

        Assert.Equal(18, planBasePrice.GetPrecision());
        Assert.Equal(4, planBasePrice.GetScale());
    }

    [Fact]
    public void ServicePlanSlug_IsUniquePerService()
    {
        var planType = _context.Model.FindEntityType(typeof(ServicePlan))!;

        Assert.Contains(
            planType.GetIndexes(),
            index => index.IsUnique &&
                     index.Properties.Select(x => x.Name)
                         .SequenceEqual(new[]
                         {
                             nameof(ServicePlan.ServiceId),
                             nameof(ServicePlan.Slug)
                         }));
    }

    [Fact]
    public void RequiredFieldKey_IsUniquePerService()
    {
        var fieldType = _context.Model.FindEntityType(typeof(ServiceRequiredField))!;

        Assert.Contains(
            fieldType.GetIndexes(),
            index => index.IsUnique &&
                     index.Properties.Select(x => x.Name)
                         .SequenceEqual(new[]
                         {
                             "ServiceId",
                             "FulfillmentMode",
                             "Actor",
                             "FieldKey"
                         }));
    }

    [Fact]
    public void CatalogRelationships_UseExpectedDeleteBehavior()
    {
        var serviceType = _context.Model.FindEntityType(typeof(DigitalService))!;
        var planType = _context.Model.FindEntityType(typeof(ServicePlan))!;
        var fieldType = _context.Model.FindEntityType(typeof(ServiceRequiredField))!;

        Assert.Equal(DeleteBehavior.Restrict, serviceType.GetForeignKeys().Single().DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade, planType.GetForeignKeys().Single().DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade, fieldType.GetForeignKeys().Single().DeleteBehavior);
    }

    [Fact]
    public void PricingEntities_UseClientGeneratedIdsAndExpectedPrecision()
    {
        AssertClientGeneratedId<ExchangeRate>();
        AssertClientGeneratedId<ProfitMargin>();

        var exchangeRateType = _context.Model.FindEntityType(typeof(ExchangeRate))!;
        var profitMarginType = _context.Model.FindEntityType(typeof(ProfitMargin))!;

        var rate = exchangeRateType.FindProperty(nameof(ExchangeRate.RateToIrr))!;
        var percentage = profitMarginType.FindProperty(nameof(ProfitMargin.Percentage))!;

        Assert.Equal(18, rate.GetPrecision());
        Assert.Equal(4, rate.GetScale());
        Assert.Equal(5, percentage.GetPrecision());
        Assert.Equal(2, percentage.GetScale());
    }

    [Fact]
    public void ProfitMargin_HasScopeAndPercentageConstraints()
    {
        var designTimeModel = _context.GetService<IDesignTimeModel>().Model;
        var profitMarginType = designTimeModel.FindEntityType(typeof(ProfitMargin))!;
        var constraintNames = profitMarginType
            .GetCheckConstraints()
            .Select(x => x.Name)
            .ToArray();

        Assert.Contains("CK_ProfitMargins_Percentage_Range", constraintNames);
        Assert.Contains("CK_ProfitMargins_SingleScope", constraintNames);
        Assert.All(
            profitMarginType.GetForeignKeys(),
            foreignKey => Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    private void AssertClientGeneratedId<TEntity>()
    {
        var entityType = _context.Model.FindEntityType(typeof(TEntity))!;
        var idProperty = entityType.FindProperty(nameof(BaseEntity.Id))!;

        Assert.Equal(ValueGenerated.Never, idProperty.ValueGenerated);
    }
}
