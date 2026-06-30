using ServiceBridge.Application.DTOs;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class PricingResolverTests
{
    private static readonly DateTime AsOfUtc =
        new(2026, 6, 21, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ResolveAsync_WhenServiceMarginExists_UsesServiceScope()
    {
        var repository = CreateRepository(
            serviceMargin: 12m,
            categoryMargin: 8m,
            globalMargin: 5m);
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveAsync(
            repository.ServiceContext!.ServiceId,
            AsOfUtc);

        Assert.True(result.Success);
        Assert.Equal(150_000m, result.Data?.ExchangeRate);
        Assert.Equal(12m, result.Data?.MarginPercentage);
        Assert.Equal(1, repository.ServiceMarginCallCount);
        Assert.Equal(0, repository.CategoryMarginCallCount);
        Assert.Equal(0, repository.GlobalMarginCallCount);
    }

    [Fact]
    public async Task ResolveAsync_WhenServiceMarginIsMissing_UsesCategoryScope()
    {
        var repository = CreateRepository(
            serviceMargin: null,
            categoryMargin: 8m,
            globalMargin: 5m);
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveAsync(
            repository.ServiceContext!.ServiceId,
            AsOfUtc);

        Assert.True(result.Success);
        Assert.Equal(8m, result.Data?.MarginPercentage);
        Assert.Equal(1, repository.ServiceMarginCallCount);
        Assert.Equal(1, repository.CategoryMarginCallCount);
        Assert.Equal(0, repository.GlobalMarginCallCount);
    }

    [Fact]
    public async Task ResolveAsync_WhenServiceMarginIsZero_DoesNotFallBack()
    {
        var repository = CreateRepository(
            serviceMargin: 0m,
            categoryMargin: 8m,
            globalMargin: 5m);
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveAsync(
            repository.ServiceContext!.ServiceId,
            AsOfUtc);

        Assert.True(result.Success);
        Assert.Equal(0m, result.Data?.MarginPercentage);
        Assert.Equal(0, repository.CategoryMarginCallCount);
        Assert.Equal(0, repository.GlobalMarginCallCount);
    }

    [Fact]
    public async Task ResolveAsync_WhenSpecificMarginsAreMissing_UsesGlobalScope()
    {
        var repository = CreateRepository(
            serviceMargin: null,
            categoryMargin: null,
            globalMargin: 5m);
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveAsync(
            repository.ServiceContext!.ServiceId,
            AsOfUtc);

        Assert.True(result.Success);
        Assert.Equal(5m, result.Data?.MarginPercentage);
        Assert.Equal(1, repository.ServiceMarginCallCount);
        Assert.Equal(1, repository.CategoryMarginCallCount);
        Assert.Equal(1, repository.GlobalMarginCallCount);
    }

    [Fact]
    public async Task ResolveAsync_WhenServiceDoesNotExist_StopsBeforePricingQueries()
    {
        var repository = new FakePricingRepository();
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveAsync(Guid.NewGuid(), AsOfUtc);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.NotFound, result.ErrorCode);
        Assert.Equal(0, repository.ExchangeRateCallCount);
        Assert.Equal(0, repository.ServiceMarginCallCount);
    }

    [Fact]
    public async Task ResolveAsync_WhenExchangeRateIsMissing_StopsBeforeMarginQuery()
    {
        var repository = CreateRepository(
            serviceMargin: 10m,
            categoryMargin: null,
            globalMargin: null);
        repository.ExchangeRate = null;
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveAsync(
            repository.ServiceContext!.ServiceId,
            AsOfUtc);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(0, repository.ServiceMarginCallCount);
    }

    [Fact]
    public async Task ResolveAsync_WhenNoMarginExists_ReturnsNotFound()
    {
        var repository = CreateRepository(null, null, null);
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveAsync(
            repository.ServiceContext!.ServiceId,
            AsOfUtc);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task ResolveAsync_WhenDateIsNotUtc_ReturnsValidationFailure()
    {
        var repository = CreateRepository(10m, null, null);
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveAsync(
            repository.ServiceContext!.ServiceId,
            DateTime.SpecifyKind(AsOfUtc, DateTimeKind.Local));

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.ValidationFailed, result.ErrorCode);
        Assert.Equal(0, repository.ServiceContextCallCount);
    }

    [Fact]
    public async Task ResolveVariantAsync_WhenVariantMarginExists_UsesVariantScope()
    {
        var repository = CreateVariantRepository(
            variantMargin: 15m,
            serviceMargin: 12m,
            categoryMargin: 8m,
            globalMargin: 5m);
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveVariantAsync(
            repository.VariantContext!.ServiceVariantId!.Value,
            AsOfUtc);

        Assert.True(result.Success);
        Assert.Equal(15m, result.Data?.MarginPercent);
        Assert.Equal("ServiceVariant", result.Data?.MarginSource);
        Assert.Equal("ServiceVariant", result.Data?.PricingSource);
        Assert.Equal(1, repository.VariantMarginCallCount);
        Assert.Equal(0, repository.ServiceMarginCallCount);
        Assert.Equal(0, repository.CategoryMarginCallCount);
        Assert.Equal(0, repository.GlobalMarginCallCount);
    }

    [Fact]
    public async Task ResolveVariantAsync_WhenVariantMarginIsMissing_UsesServiceScope()
    {
        var repository = CreateVariantRepository(
            variantMargin: null,
            serviceMargin: 12m,
            categoryMargin: 8m,
            globalMargin: 5m);
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveVariantAsync(
            repository.VariantContext!.ServiceVariantId!.Value,
            AsOfUtc);

        Assert.True(result.Success);
        Assert.Equal(12m, result.Data?.MarginPercent);
        Assert.Equal("DigitalService", result.Data?.MarginSource);
        Assert.Equal(1, repository.VariantMarginCallCount);
        Assert.Equal(1, repository.ServiceMarginCallCount);
        Assert.Equal(0, repository.CategoryMarginCallCount);
        Assert.Equal(0, repository.GlobalMarginCallCount);
    }

    [Fact]
    public async Task ResolveVariantAsync_WhenServiceMarginIsMissing_UsesCategoryScope()
    {
        var repository = CreateVariantRepository(
            variantMargin: null,
            serviceMargin: null,
            categoryMargin: 8m,
            globalMargin: 5m);
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveVariantAsync(
            repository.VariantContext!.ServiceVariantId!.Value,
            AsOfUtc);

        Assert.True(result.Success);
        Assert.Equal(8m, result.Data?.MarginPercent);
        Assert.Equal("Category", result.Data?.MarginSource);
        Assert.Equal(1, repository.VariantMarginCallCount);
        Assert.Equal(1, repository.ServiceMarginCallCount);
        Assert.Equal(1, repository.CategoryMarginCallCount);
        Assert.Equal(0, repository.GlobalMarginCallCount);
    }

    [Fact]
    public async Task ResolveVariantAsync_WhenSpecificMarginsAreMissing_UsesGlobalScope()
    {
        var repository = CreateVariantRepository(
            variantMargin: null,
            serviceMargin: null,
            categoryMargin: null,
            globalMargin: 5m);
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveVariantAsync(
            repository.VariantContext!.ServiceVariantId!.Value,
            AsOfUtc);

        Assert.True(result.Success);
        Assert.Equal(5m, result.Data?.MarginPercent);
        Assert.Equal("Global", result.Data?.MarginSource);
        Assert.Equal(1, repository.GlobalMarginCallCount);
    }

    [Fact]
    public async Task ResolveVariantAsync_UsesVariantCurrencyForExchangeRate()
    {
        var repository = CreateVariantRepository(
            variantMargin: 10m,
            serviceMargin: null,
            categoryMargin: null,
            globalMargin: null);
        repository.VariantContext!.CurrencyCode = "eur";
        repository.ExchangeRate = 170_000m;
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveVariantAsync(
            repository.VariantContext.ServiceVariantId!.Value,
            AsOfUtc);

        Assert.True(result.Success);
        Assert.Equal("EUR", repository.RequestedCurrencyCode);
        Assert.Equal("EUR", result.Data?.CurrencyCode);
        Assert.Equal("EUR", result.Data?.ExchangeRateSource);
    }

    [Fact]
    public async Task ResolveVariantAsync_WhenVariantDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakePricingRepository();
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveVariantAsync(Guid.NewGuid(), AsOfUtc);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.NotFound, result.ErrorCode);
        Assert.Equal(0, repository.ExchangeRateCallCount);
    }

    [Fact]
    public async Task ResolveVariantAsync_WhenVariantIsInactive_ReturnsValidationFailure()
    {
        var repository = CreateVariantRepository(10m, null, null, null);
        repository.VariantContext!.IsVariantActive = false;
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveVariantAsync(
            repository.VariantContext.ServiceVariantId!.Value,
            AsOfUtc);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.ValidationFailed, result.ErrorCode);
        Assert.Equal(0, repository.ExchangeRateCallCount);
    }

    [Fact]
    public async Task ResolveVariantAsync_WhenServiceIsInactive_ReturnsNotFound()
    {
        var repository = CreateVariantRepository(10m, null, null, null);
        repository.VariantContext!.IsServiceActive = false;
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveVariantAsync(
            repository.VariantContext.ServiceVariantId!.Value,
            AsOfUtc);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(0, repository.ExchangeRateCallCount);
    }

    [Fact]
    public async Task ResolveVariantAsync_WhenCategoryIsInactive_ReturnsNotFound()
    {
        var repository = CreateVariantRepository(10m, null, null, null);
        repository.VariantContext!.IsCategoryActive = false;
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveVariantAsync(
            repository.VariantContext.ServiceVariantId!.Value,
            AsOfUtc);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(0, repository.ExchangeRateCallCount);
    }

    [Fact]
    public async Task ResolveVariantAsync_WhenCurrencyIsUnsupported_ReturnsValidationFailure()
    {
        var repository = CreateVariantRepository(10m, null, null, null);
        repository.VariantContext!.CurrencyCode = "XYZ";
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveVariantAsync(
            repository.VariantContext.ServiceVariantId!.Value,
            AsOfUtc);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.ValidationFailed, result.ErrorCode);
        Assert.Equal(0, repository.ExchangeRateCallCount);
    }

    [Fact]
    public async Task ResolveVariantAsync_WhenExchangeRateIsMissing_ReturnsNotFound()
    {
        var repository = CreateVariantRepository(10m, null, null, null);
        repository.ExchangeRate = null;
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveVariantAsync(
            repository.VariantContext!.ServiceVariantId!.Value,
            AsOfUtc);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(0, repository.VariantMarginCallCount);
    }

    [Fact]
    public async Task ResolveVariantAsync_WhenProfitMarginIsMissing_ReturnsNotFound()
    {
        var repository = CreateVariantRepository(null, null, null, null);
        var resolver = CreateResolver(repository);

        var result = await resolver.ResolveVariantAsync(
            repository.VariantContext!.ServiceVariantId!.Value,
            AsOfUtc);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.NotFound, result.ErrorCode);
    }

    private static FakePricingRepository CreateRepository(
        decimal? serviceMargin,
        decimal? categoryMargin,
        decimal? globalMargin)
    {
        return new FakePricingRepository
        {
            ServiceContext = new ServicePricingContext
            {
                ServiceId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                CurrencyCode = "USD",
                BasePrice = 20m
            },
            ExchangeRate = 150_000m,
            ServiceMargin = serviceMargin,
            CategoryMargin = categoryMargin,
            GlobalMargin = globalMargin
        };
    }

    private static FakePricingRepository CreateVariantRepository(
        decimal? variantMargin,
        decimal? serviceMargin,
        decimal? categoryMargin,
        decimal? globalMargin)
    {
        var variantId = Guid.NewGuid();
        return new FakePricingRepository
        {
            VariantContext = new ServicePricingContext
            {
                ServiceId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                ServiceVariantId = variantId,
                RegionCode = "GLOBAL",
                CurrencyCode = "USD",
                BasePrice = 25m
            },
            ExchangeRate = 150_000m,
            VariantMargin = variantMargin,
            ServiceMargin = serviceMargin,
            CategoryMargin = categoryMargin,
            GlobalMargin = globalMargin
        };
    }

    private static PricingResolver CreateResolver(FakePricingRepository repository)
    {
        return new PricingResolver(repository, new PricingCalculator());
    }
}
