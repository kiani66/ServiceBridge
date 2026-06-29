using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
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
        AssertClientGeneratedId<Provider>();
        AssertClientGeneratedId<ServiceCategory>();
        AssertClientGeneratedId<DigitalService>();
        AssertClientGeneratedId<ServiceVariant>();
        AssertClientGeneratedId<ServiceRequiredField>();
    }

    [Fact]
    public void ServiceVariant_ReplacesServicePlanAsDomainType()
    {
        Assert.Equal("ServiceVariant", typeof(ServiceVariant).Name);
        Assert.Null(Type.GetType("ServiceBridge.Domain.Entities.ServicePlan, ServiceBridge.Domain"));
    }

    [Fact]
    public void DigitalService_ExposesVariantsNavigation()
    {
        var variants = typeof(DigitalService).GetProperty(nameof(DigitalService.Variants));

        Assert.NotNull(variants);
        Assert.Equal(typeof(ICollection<ServiceVariant>), variants.PropertyType);
        Assert.Null(typeof(DigitalService).GetProperty("Plans"));
    }

    [Fact]
    public void CategoryProviderAndServiceSlugs_AreUnique()
    {
        var providerType = _context.Model.FindEntityType(typeof(Provider))!;
        var categoryType = _context.Model.FindEntityType(typeof(ServiceCategory))!;
        var serviceType = _context.Model.FindEntityType(typeof(DigitalService))!;

        Assert.Contains(
            providerType.GetIndexes(),
            index => index.IsUnique &&
                     index.Properties.Select(x => x.Name).SequenceEqual(new[] { "Slug" }));

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
    public void Provider_RequiresNameSlugAndDefaultsActive()
    {
        var providerType = _context.Model.FindEntityType(typeof(Provider))!;

        Assert.False(providerType.FindProperty(nameof(Provider.Name))!.IsNullable);
        Assert.False(providerType.FindProperty(nameof(Provider.Slug))!.IsNullable);
        Assert.Equal(true, providerType.FindProperty(nameof(Provider.IsActive))!.GetDefaultValue());
    }

    [Fact]
    public void ServiceBasePrice_UsesExpectedPrecision()
    {
        var serviceType = _context.Model.FindEntityType(typeof(DigitalService))!;
        var basePrice = serviceType.FindProperty(nameof(DigitalService.BasePrice))!;

        Assert.Equal(18, basePrice.GetPrecision());
        Assert.Equal(4, basePrice.GetScale());

        var variantType = _context.Model.FindEntityType(typeof(ServiceVariant))!;
        var variantBasePrice = variantType.FindProperty(nameof(ServiceVariant.BasePrice))!;

        Assert.Equal(18, variantBasePrice.GetPrecision());
        Assert.Equal(4, variantBasePrice.GetScale());
    }

    [Fact]
    public void ServiceVariantSlug_IsUniquePerService()
    {
        var variantType = _context.Model.FindEntityType(typeof(ServiceVariant))!;

        Assert.Contains(
            variantType.GetIndexes(),
            index => index.IsUnique &&
                     index.Properties.Select(x => x.Name)
                         .SequenceEqual(new[]
                          {
                              nameof(ServiceVariant.ServiceId),
                              nameof(ServiceVariant.Slug)
                         }));
    }

    [Fact]
    public void ServiceVariant_HasBasicStructuredFields()
    {
        var variantType = _context.Model.FindEntityType(typeof(ServiceVariant))!;
        var billingCycle = variantType.FindProperty(nameof(ServiceVariant.BillingCycle))!;
        var accountDeliveryType =
            variantType.FindProperty(nameof(ServiceVariant.AccountDeliveryType))!;
        var currencyCode = variantType.FindProperty(nameof(ServiceVariant.CurrencyCode))!;
        var regionCode = variantType.FindProperty(nameof(ServiceVariant.RegionCode))!;
        var fulfillmentMode = variantType.FindProperty(nameof(ServiceVariant.FulfillmentMode))!;
        var variant = new ServiceVariant();

        Assert.NotNull(typeof(ServiceVariant).GetProperty(nameof(ServiceVariant.BillingCycle)));
        Assert.NotNull(typeof(ServiceVariant).GetProperty(nameof(ServiceVariant.AccountDeliveryType)));
        Assert.NotNull(typeof(ServiceVariant).GetProperty(nameof(ServiceVariant.CurrencyCode)));
        Assert.NotNull(typeof(ServiceVariant).GetProperty(nameof(ServiceVariant.RegionCode)));
        Assert.NotNull(typeof(ServiceVariant).GetProperty(nameof(ServiceVariant.FulfillmentMode)));
        Assert.False(billingCycle.IsNullable);
        Assert.False(accountDeliveryType.IsNullable);
        Assert.False(currencyCode.IsNullable);
        Assert.False(regionCode.IsNullable);
        Assert.False(fulfillmentMode.IsNullable);
        Assert.Equal(BillingCycle.None, billingCycle.GetDefaultValue());
        Assert.Equal(
            AccountDeliveryType.NotApplicable,
            accountDeliveryType.GetDefaultValue());
        Assert.Equal("USD", currencyCode.GetDefaultValue());
        Assert.Equal("GLOBAL", regionCode.GetDefaultValue());
        Assert.Equal(FulfillmentMode.ManualAgent, fulfillmentMode.GetDefaultValue());
        Assert.Equal(3, currencyCode.GetMaxLength());
        Assert.Equal(20, regionCode.GetMaxLength());
        Assert.Equal("USD", variant.CurrencyCode);
        Assert.Equal("GLOBAL", variant.RegionCode);
        Assert.Equal(FulfillmentMode.ManualAgent, variant.FulfillmentMode);
    }

    [Fact]
    public void ServiceVariantStructuredFields_HaveValidValueCheckConstraints()
    {
        var designTimeModel = _context.GetService<IDesignTimeModel>().Model;
        var variantType = designTimeModel.FindEntityType(typeof(ServiceVariant))!;
        var constraintNames = variantType
            .GetCheckConstraints()
            .Select(x => x.Name)
            .ToArray();

        Assert.Contains(
            "CK_ServiceVariants_BillingCycle_Valid",
            constraintNames);
        Assert.Contains(
            "CK_ServiceVariants_AccountDeliveryType_Valid",
            constraintNames);
        Assert.Contains(
            "CK_ServiceVariants_FulfillmentMode_Valid",
            constraintNames);
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
        var variantType = _context.Model.FindEntityType(typeof(ServiceVariant))!;
        var fieldType = _context.Model.FindEntityType(typeof(ServiceRequiredField))!;

        Assert.Contains(serviceType.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(x => x.Name).SequenceEqual(new[] { "CategoryId" }) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(serviceType.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(x => x.Name).SequenceEqual(new[] { "ProviderId" }) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Equal(DeleteBehavior.Cascade, variantType.GetForeignKeys().Single().DeleteBehavior);
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
