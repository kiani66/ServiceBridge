using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.ValueObjects;
using Xunit;

namespace ServiceBridge.Tests.CatalogVocabulary;

public class CatalogVocabularyTests
{
    [Theory]
    [InlineData(CurrencyCode.USD, "USD")]
    [InlineData(CurrencyCode.EUR, "EUR")]
    [InlineData(CurrencyCode.GBP, "GBP")]
    [InlineData(CurrencyCode.AED, "AED")]
    [InlineData(CurrencyCode.TRY, "TRY")]
    public void CurrencyCode_RoundTripsIsoCode(CurrencyCode currencyCode, string isoCode)
    {
        Assert.Equal(isoCode, currencyCode.ToIsoCode());
        Assert.Equal(isoCode, currencyCode.ToIsoString());
        Assert.Equal(currencyCode, CurrencyCodeExtensions.FromIsoCode(isoCode.ToLowerInvariant()));
        Assert.Equal(currencyCode, CurrencyCodeExtensions.FromIsoString(isoCode.ToLowerInvariant()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("CAD")]
    public void CurrencyCode_FromIsoCodeRejectsUnsupportedCodes(string isoCode)
    {
        Assert.False(CurrencyCodeExtensions.TryFromIsoCode(isoCode, out var currencyCode));
        Assert.Equal(CurrencyCode.Unknown, currencyCode);
        Assert.False(CurrencyCodeExtensions.TryFromIsoString(isoCode, out currencyCode));
        Assert.Equal(CurrencyCode.Unknown, currencyCode);
        Assert.Throws<ArgumentException>(() => CurrencyCodeExtensions.FromIsoCode(isoCode));
        Assert.Throws<ArgumentException>(() => CurrencyCodeExtensions.FromIsoString(isoCode));
    }

    [Fact]
    public void RegionCode_NormalizesCode()
    {
        var regionCode = RegionCode.FromCode(" global ");

        Assert.Equal("GLOBAL", regionCode.Code);
        Assert.Equal("Global", regionCode.DisplayName);
        Assert.Equal("GLOBAL", regionCode.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegionCode_RejectsEmptyCode(string code)
    {
        Assert.Throws<ArgumentException>(() => RegionCode.FromCode(code));
    }

    [Fact]
    public void BillingCycle_ValuesAreStable()
    {
        Assert.Equal(0, (int)BillingCycle.None);
        Assert.Equal(1, (int)BillingCycle.OneTime);
        Assert.Equal(2, (int)BillingCycle.Monthly);
        Assert.Equal(3, (int)BillingCycle.Quarterly);
        Assert.Equal(4, (int)BillingCycle.SemiAnnual);
        Assert.Equal(5, (int)BillingCycle.Annual);
        Assert.Equal(6, (int)BillingCycle.CustomDays);
    }

    [Fact]
    public void AccountDeliveryType_ValuesAreStable()
    {
        Assert.Equal(0, (int)AccountDeliveryType.NotApplicable);
        Assert.Equal(1, (int)AccountDeliveryType.UseExistingAccount);
        Assert.Equal(2, (int)AccountDeliveryType.CreateNewAccount);
        Assert.Equal(3, (int)AccountDeliveryType.ActivationCode);
        Assert.Equal(4, (int)AccountDeliveryType.GiftCardCode);
        Assert.Equal(5, (int)AccountDeliveryType.InstructionOnly);
    }

    [Fact]
    public void FulfillmentMode_ValuesAreStable()
    {
        Assert.Equal(1, (int)FulfillmentMode.ManualAgent);
        Assert.Equal(2, (int)FulfillmentMode.InventoryCode);
        Assert.Equal(3, (int)FulfillmentMode.ExternalAutomation);
        Assert.Equal(4, (int)FulfillmentMode.InstructionOnly);
    }
}
