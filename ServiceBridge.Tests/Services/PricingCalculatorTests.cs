using System.Reflection;
using ServiceBridge.Infrastructure.Services;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class PricingCalculatorTests
{
    private readonly PricingCalculator _calculator = new();

    [Fact]
    public void Calculate_UsesExpectedBusinessFormula()
    {
        var result = _calculator.Calculate(
            foreignPrice: 20m,
            exchangeRate: 150_000m,
            marginPercent: 10m);

        Assert.Equal(3_000_000m, result.BaseAmountIrr);
        Assert.Equal(300_000m, result.ProfitAmountIrr);
        Assert.Equal(3_300_000m, result.FinalAmountIrr);
    }

    [Fact]
    public void Calculate_RoundsIrrAwayFromZero()
    {
        var result = _calculator.Calculate(
            foreignPrice: 1.005m,
            exchangeRate: 100m,
            marginPercent: 2.5m);

        Assert.Equal(101m, result.BaseAmountIrr);
        Assert.Equal(3m, result.ProfitAmountIrr);
        Assert.Equal(104m, result.FinalAmountIrr);
    }

    [Fact]
    public void PricingCalculator_HasNoInstanceDependencies()
    {
        var fields = typeof(PricingCalculator).GetFields(
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public);

        Assert.Empty(fields);
    }

    [Theory]
    [InlineData(-1, 100, 10)]
    [InlineData(1, 0, 10)]
    [InlineData(1, 100, -1)]
    [InlineData(1, 100, 101)]
    public void Calculate_WhenInputIsOutsideAllowedRange_Throws(
        decimal foreignPrice,
        decimal exchangeRate,
        decimal marginPercent)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.Calculate(foreignPrice, exchangeRate, marginPercent));
    }
}
