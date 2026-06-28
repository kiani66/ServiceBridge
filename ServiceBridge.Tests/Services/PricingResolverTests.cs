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
        var resolver = new PricingResolver(repository);

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
        var resolver = new PricingResolver(repository);

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
        var resolver = new PricingResolver(repository);

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
        var resolver = new PricingResolver(repository);

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
        var resolver = new PricingResolver(repository);

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
        var resolver = new PricingResolver(repository);

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
        var resolver = new PricingResolver(repository);

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
        var resolver = new PricingResolver(repository);

        var result = await resolver.ResolveAsync(
            repository.ServiceContext!.ServiceId,
            DateTime.SpecifyKind(AsOfUtc, DateTimeKind.Local));

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.ValidationFailed, result.ErrorCode);
        Assert.Equal(0, repository.ServiceContextCallCount);
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
                CurrencyCode = "USD"
            },
            ExchangeRate = 150_000m,
            ServiceMargin = serviceMargin,
            CategoryMargin = categoryMargin,
            GlobalMargin = globalMargin
        };
    }
}
