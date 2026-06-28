using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class AdminPricingServiceTests
{
    private static readonly DateTime EffectiveUtc =
        new(2026, 6, 21, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateExchangeRateAsync_NormalizesCurrencyCode()
    {
        var repository = new FakeAdminPricingRepository();
        var service = new AdminPricingService(repository);

        var result = await service.CreateExchangeRateAsync(new CreateExchangeRateRequest
        {
            CurrencyCode = "usd",
            RateToIrr = 150_000m,
            EffectiveFromUtc = EffectiveUtc
        });

        Assert.True(result.Success);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("USD", result.Data?.CurrencyCode);
    }

    [Fact]
    public async Task CreateExchangeRateAsync_WhenDateIsNotUtc_ReturnsValidationFailure()
    {
        var repository = new FakeAdminPricingRepository();
        var service = new AdminPricingService(repository);

        var result = await service.CreateExchangeRateAsync(new CreateExchangeRateRequest
        {
            CurrencyCode = "USD",
            RateToIrr = 150_000m,
            EffectiveFromUtc = DateTime.SpecifyKind(EffectiveUtc, DateTimeKind.Unspecified)
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task CreateProfitMarginAsync_WhenServiceExists_CreatesServiceScope()
    {
        var repository = new FakeAdminPricingRepository();
        var serviceId = Guid.NewGuid();
        repository.SeedService(serviceId);
        var service = new AdminPricingService(repository);

        var result = await service.CreateProfitMarginAsync(new CreateProfitMarginRequest
        {
            ServiceId = serviceId,
            Percentage = 10m,
            EffectiveFromUtc = EffectiveUtc
        });

        Assert.True(result.Success);
        Assert.Equal("Service", result.Data?.Scope);
        Assert.Equal(serviceId, result.Data?.ServiceId);
    }

    [Fact]
    public async Task CreateProfitMarginAsync_WhenBothScopesAreSet_ReturnsValidationFailure()
    {
        var repository = new FakeAdminPricingRepository();
        var service = new AdminPricingService(repository);

        var result = await service.CreateProfitMarginAsync(new CreateProfitMarginRequest
        {
            ServiceId = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Percentage = 10m,
            EffectiveFromUtc = EffectiveUtc
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.ValidationFailed, result.ErrorCode);
    }

    [Fact]
    public async Task CreateProfitMarginAsync_WhenTargetDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeAdminPricingRepository();
        var service = new AdminPricingService(repository);

        var result = await service.CreateProfitMarginAsync(new CreateProfitMarginRequest
        {
            CategoryId = Guid.NewGuid(),
            Percentage = 10m,
            EffectiveFromUtc = EffectiveUtc
        });

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task UpdateExchangeRateStatusAsync_UpdatesExistingRecord()
    {
        var repository = new FakeAdminPricingRepository();
        var exchangeRate = new ExchangeRate
        {
            CurrencyCode = "USD",
            RateToIrr = 150_000m,
            IsActive = true,
            EffectiveFrom = EffectiveUtc
        };
        repository.SeedExchangeRate(exchangeRate);
        var service = new AdminPricingService(repository);

        var result = await service.UpdateExchangeRateStatusAsync(
            exchangeRate.Id,
            new UpdatePricingStatusRequest { IsActive = false });

        Assert.True(result.Success);
        Assert.False(result.Data?.IsActive);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }
}
