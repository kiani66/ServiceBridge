using Microsoft.Extensions.Logging.Abstractions;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class ServiceCatalogServiceTests
{
    [Fact]
    public async Task GetActiveServiceByIdAsync_WhenServiceExists_MapsFieldsInDisplayOrder()
    {
        var repository = new FakeServiceCatalogRepository();
        var service = CreateDigitalService();
        service.RequiredFields.Add(CreateField(service, "password", 2, true));
        service.RequiredFields.Add(CreateField(service, "email", 1, false));
        repository.SeedService(service);
        var catalogService = CreateQueryService(repository);

        var result = await catalogService.GetActiveServiceByIdAsync(service.Id);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(new[] { "email", "password" },
            result.Data.RequiredFields.Select(x => x.FieldKey));
        Assert.Equal("Password", result.Data.RequiredFields.Last().FieldType);
        Assert.True(result.Data.RequiredFields.Last().IsSensitive);
        Assert.Equal("OpenAI", result.Data.Provider);
    }

    [Fact]
    public async Task GetActiveServiceByIdAsync_WhenServiceIsInactive_ReturnsNotFound()
    {
        var repository = new FakeServiceCatalogRepository();
        var service = CreateDigitalService();
        service.IsActive = false;
        repository.SeedService(service);
        var catalogService = CreateQueryService(repository);

        var result = await catalogService.GetActiveServiceByIdAsync(service.Id);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.NotFound, result.ErrorCode);
    }

    [Fact]
    public async Task GetAllServicesAsync_IncludesInactiveServicesForAdmin()
    {
        var repository = new FakeServiceCatalogRepository();
        var service = CreateDigitalService();
        service.IsActive = false;
        repository.SeedService(service);
        var catalogService = CreateQueryService(repository);

        var result = await catalogService.GetAllServicesAsync();

        Assert.True(result.Success);
        var response = Assert.Single(result.Data!);
        Assert.False(response.IsActive);
    }

    [Fact]
    public async Task CreateCategoryAsync_WhenSlugExists_ReturnsConflict()
    {
        var repository = new FakeServiceCatalogRepository();
        repository.SeedCategory(new ServiceCategory
        {
            Name = "AI",
            Slug = "ai-tools"
        });
        var catalogService = CreateAdminService(repository);

        var result = await catalogService.CreateCategoryAsync(new CreateServiceCategoryRequest
        {
            Name = "Other AI",
            Slug = "AI-TOOLS"
        });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
    }

    [Fact]
    public async Task CreateServiceAsync_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var repository = new FakeServiceCatalogRepository();
        var catalogService = CreateAdminService(repository);

        var result = await catalogService.CreateServiceAsync(
            CreateServiceRequest(Guid.NewGuid()));

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Fact]
    public async Task CreateServiceAsync_WhenValid_NormalizesSlugAndCurrency()
    {
        var repository = new FakeServiceCatalogRepository();
        var category = new ServiceCategory { Name = "AI", Slug = "ai" };
        repository.SeedCategory(category);
        var catalogService = CreateAdminService(repository);
        var request = CreateServiceRequest(category.Id);
        request.Slug = "ChatGPT-Plus";
        request.CurrencyCode = "usd";

        var result = await catalogService.CreateServiceAsync(request);

        Assert.True(result.Success);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("chatgpt-plus", result.Data?.Slug);
        Assert.Equal("USD", result.Data?.CurrencyCode);
        Assert.Equal(category.Name, result.Data?.CategoryName);
        Assert.Equal("OpenAI", result.Data?.Provider);
        Assert.NotNull(repository.FindProviderBySlug("openai"));
    }

    [Fact]
    public async Task CreateServiceAsync_WhenProviderTextHasSeparators_NormalizesProviderSlug()
    {
        var repository = new FakeServiceCatalogRepository();
        var category = new ServiceCategory { Name = "AI", Slug = "ai" };
        repository.SeedCategory(category);
        var catalogService = CreateAdminService(repository);
        var request = CreateServiceRequest(category.Id);
        request.Provider = "Open AI, Inc.";

        var result = await catalogService.CreateServiceAsync(request);

        Assert.True(result.Success);
        Assert.Equal("Open AI, Inc.", result.Data?.Provider);
        Assert.NotNull(repository.FindProviderBySlug("open-ai-inc"));
    }

    [Fact]
    public async Task UpdateServiceAsync_WhenProviderTextChanges_ResolvesProvider()
    {
        var repository = new FakeServiceCatalogRepository();
        var service = CreateDigitalService();
        repository.SeedService(service);
        var catalogService = CreateAdminService(repository);

        var result = await catalogService.UpdateServiceAsync(
            service.Id,
            new UpdateDigitalServiceRequest
            {
                CategoryId = service.CategoryId,
                Name = "ChatGPT Plus",
                Provider = "Open AI, Inc.",
                Slug = "chatgpt-plus",
                CurrencyCode = "USD",
                BasePrice = 20,
                IsActive = true,
                SupportsUseExistingAccount = true,
                SupportsCreateNewAccount = false
            });

        Assert.True(result.Success);
        Assert.Equal("Open AI, Inc.", result.Data?.Provider);
        Assert.Equal("open-ai-inc", service.Provider.Slug);
    }

    [Fact]
    public async Task CreateRequiredFieldAsync_WhenKeyExists_ReturnsConflict()
    {
        var repository = new FakeServiceCatalogRepository();
        var service = CreateDigitalService();
        service.RequiredFields.Add(CreateField(service, "email", 1, false));
        repository.SeedService(service);
        var catalogService = CreateAdminService(repository);

        var result = await catalogService.CreateRequiredFieldAsync(
            service.Id,
            new CreateServiceRequiredFieldRequest
            {
                FieldKey = "EMAIL",
                Label = "Account email",
                FieldType = ServiceFieldType.Email,
                IsRequired = true
            });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task CreatePlanAsync_WhenValid_NormalizesSlugRegionAndCurrency()
    {
        var repository = new FakeServiceCatalogRepository();
        var service = CreateDigitalService();
        repository.SeedService(service);
        var catalogService = CreateAdminService(repository);

        var result = await catalogService.CreatePlanAsync(
            service.Id,
            new CreateServicePlanRequest
            {
                Name = "Monthly / Global",
                Slug = "Monthly-Global",
                DurationDays = 30,
                RegionCode = "global",
                CurrencyCode = "usd",
                BasePrice = 20,
                IsActive = true,
                DisplayOrder = 1
            });

        Assert.True(result.Success);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("monthly-global", result.Data?.Slug);
        Assert.Equal("GLOBAL", result.Data?.RegionCode);
        Assert.Equal("USD", result.Data?.CurrencyCode);
        Assert.Single(service.Plans);
    }

    [Fact]
    public async Task CreatePlanAsync_WhenSlugExistsForService_ReturnsConflict()
    {
        var repository = new FakeServiceCatalogRepository();
        var service = CreateDigitalService();
        service.Plans.Add(CreatePlan(service, "monthly-global"));
        repository.SeedService(service);
        var catalogService = CreateAdminService(repository);

        var result = await catalogService.CreatePlanAsync(
            service.Id,
            new CreateServicePlanRequest
            {
                Name = "Monthly",
                Slug = "MONTHLY-GLOBAL",
                CurrencyCode = "USD",
                BasePrice = 20,
                IsActive = true
            });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    private static ServiceCatalogQueryService CreateQueryService(
        FakeServiceCatalogRepository repository)
    {
        return new ServiceCatalogQueryService(repository);
    }

    private static AdminCatalogService CreateAdminService(
        FakeServiceCatalogRepository repository)
    {
        return new AdminCatalogService(
            repository,
            NullLogger<AdminCatalogService>.Instance);
    }

    private static DigitalService CreateDigitalService()
    {
        var category = new ServiceCategory
        {
            Name = "AI Services",
            Slug = "ai-services",
            IsActive = true
        };
        var provider = new Provider
        {
            Name = "OpenAI",
            Slug = "openai",
            IsActive = true
        };

        return new DigitalService
        {
            CategoryId = category.Id,
            Category = category,
            ProviderId = provider.Id,
            Provider = provider,
            Name = "ChatGPT Plus",
            Slug = "chatgpt-plus",
            CurrencyCode = "USD",
            BasePrice = 20,
            IsActive = true
        };
    }

    [Fact]
    public void ServicePlan_RemainsCurrentCatalogPlanType()
    {
        Assert.Equal("ServicePlan", typeof(ServicePlan).Name);
    }

    private static ServiceRequiredField CreateField(
        DigitalService service,
        string key,
        int order,
        bool sensitive)
    {
        return new ServiceRequiredField
        {
            ServiceId = service.Id,
            Service = service,
            FieldKey = key,
            Label = key,
            FieldType = sensitive ? ServiceFieldType.Password : ServiceFieldType.Email,
            IsRequired = true,
            IsSensitive = sensitive,
            DisplayOrder = order
        };
    }

    private static ServicePlan CreatePlan(DigitalService service, string slug)
    {
        return new ServicePlan
        {
            ServiceId = service.Id,
            Service = service,
            Name = "Monthly / Global",
            Slug = slug,
            CurrencyCode = "USD",
            BasePrice = 20,
            IsActive = true
        };
    }

    private static CreateDigitalServiceRequest CreateServiceRequest(Guid categoryId)
    {
        return new CreateDigitalServiceRequest
        {
            CategoryId = categoryId,
            Name = "ChatGPT Plus",
            Provider = "OpenAI",
            Slug = "chatgpt-plus",
            CurrencyCode = "USD",
            BasePrice = 20,
            IsActive = true
        };
    }
}
