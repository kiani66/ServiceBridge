using ServiceBridge.Application.DTOs;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class PricingQuoteServiceTests
{
    private static readonly DateTime QuotedAtUtc =
        new(2026, 6, 21, 11, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetQuoteAsync_ComposesContextResolverAndCalculator()
    {
        var repository = new FakePricingRepository
        {
            ServiceContext = new ServicePricingContext
            {
                ServiceId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                CurrencyCode = "USD",
                BasePrice = 20m
            },
            ExchangeRate = 150_000m,
            ServiceMargin = 10m
        };
        var resolver = new PricingResolver(repository);
        var service = new PricingQuoteService(
            repository,
            resolver,
            new PricingCalculator());

        var result = await service.GetQuoteAsync(
            repository.ServiceContext.ServiceId,
            null,
            QuotedAtUtc);

        Assert.True(result.Success);
        Assert.Equal(3_300_000m, result.Data?.FinalAmountIrr);
        Assert.Equal(1, repository.ServiceContextCallCount);
        Assert.Equal(0, repository.CategoryMarginCallCount);
    }

    [Fact]
    public async Task GetQuoteAsync_WhenServiceDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakePricingRepository();
        var resolver = new PricingResolver(repository);
        var service = new PricingQuoteService(
            repository,
            resolver,
            new PricingCalculator());

        var result = await service.GetQuoteAsync(Guid.NewGuid(), null, QuotedAtUtc);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(0, repository.ExchangeRateCallCount);
    }

    [Fact]
    public async Task GetQuoteAsync_WhenPlanIsSelected_ReturnsPlanSnapshot()
    {
        var planId = Guid.NewGuid();
        var repository = new FakePricingRepository
        {
            ServiceContext = new ServicePricingContext
            {
                ServiceId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                PlanName = "Monthly / Global",
                PlanDurationDays = 30,
                PlanRegionCode = "GLOBAL",
                CurrencyCode = "USD",
                BasePrice = 25m
            },
            ExchangeRate = 150_000m,
            ServiceMargin = 10m
        };
        var resolver = new PricingResolver(repository);
        var service = new PricingQuoteService(
            repository,
            resolver,
            new PricingCalculator());

        var result = await service.GetQuoteAsync(
            repository.ServiceContext.ServiceId,
            planId,
            QuotedAtUtc);

        Assert.True(result.Success);
        Assert.Equal(planId, result.Data?.ServiceVariantId);
        Assert.Equal(planId, result.Data?.PlanId);
        Assert.Equal("Monthly / Global", result.Data?.PlanName);
        Assert.Equal(30, result.Data?.PlanDurationDays);
        Assert.Equal("GLOBAL", result.Data?.PlanRegionCode);
        Assert.Equal(4_125_000m, result.Data?.FinalAmountIrr);
    }
}
