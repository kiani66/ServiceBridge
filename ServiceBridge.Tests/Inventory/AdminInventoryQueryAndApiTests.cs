using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using ServiceBridge.Api.Controllers;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Inventory;

public class AdminInventoryQueryAndApiTests
{
    [Fact]
    public async Task ListAsync_IsPageableAndPassesFiltersToProjectionRepository()
    {
        var repository = new FakeAdminInventoryQueryRepository();
        var service = new AdminInventoryQueryService(repository);
        var variantId = Guid.NewGuid();

        var result = await service.GetItemsAsync(
            2,
            10,
            variantId,
            Domain.Enums.InventoryItemStatus.Available);

        Assert.True(result.Success);
        var lastRequest = Assert.NotNull(repository.LastListRequest);
        Assert.Equal(2, lastRequest.Page);
        Assert.Equal(10, lastRequest.PageSize);
        Assert.Equal(variantId, lastRequest.VariantId);
        Assert.Equal(Domain.Enums.InventoryItemStatus.Available, lastRequest.Status);
    }

    [Fact]
    public async Task ListAsync_RejectsInvalidPage()
    {
        var service = new AdminInventoryQueryService(new FakeAdminInventoryQueryRepository());

        var result = await service.GetItemsAsync(0, 25, null, null);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task VariantOptionsAsync_NormalizesSearchAndPassesBoundedPageSize()
    {
        var repository = new FakeAdminInventoryQueryRepository();
        var service = new AdminInventoryQueryService(repository);

        var result = await service.GetVariantOptionsAsync("  regional code  ", 100);

        Assert.True(result.Success);
        var request = Assert.NotNull(repository.LastVariantOptionsRequest);
        Assert.Equal("regional code", request.Search);
        Assert.Equal(100, request.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public async Task VariantOptionsAsync_RejectsPageSizeOutsideSelectorBounds(int pageSize)
    {
        var service = new AdminInventoryQueryService(new FakeAdminInventoryQueryRepository());

        var result = await service.GetVariantOptionsAsync(null, pageSize);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task SummaryAsync_ReturnsEveryInventoryStatusCount()
    {
        var variantId = Guid.NewGuid();
        var repository = new FakeAdminInventoryQueryRepository
        {
            Summary = new InventoryAvailabilitySummaryDto
            {
                ServiceVariantId = variantId,
                Available = 4,
                Reserved = 3,
                Delivered = 2,
                Disabled = 1,
                Total = 10
            }
        };
        var service = new AdminInventoryQueryService(repository);

        var result = await service.GetSummaryAsync(variantId);

        Assert.True(result.Success);
        Assert.Equal(4, result.Data?.Available);
        Assert.Equal(3, result.Data?.Reserved);
        Assert.Equal(2, result.Data?.Delivered);
        Assert.Equal(1, result.Data?.Disabled);
        Assert.Equal(10, result.Data?.Total);
    }

    [Fact]
    public void InventoryListDto_DoesNotExposeProtectedValuesOrFingerprints()
    {
        var names = typeof(AdminInventoryItemDto).GetProperties().Select(x => x.Name).ToArray();

        Assert.DoesNotContain("ProtectedCode", names);
        Assert.DoesNotContain("CodeFingerprint", names);
        Assert.DoesNotContain(names, name => name.Contains("Preview", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("Suffix", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VariantOptionDto_ContainsOnlyApprovedSafeProperties()
    {
        var names = typeof(AdminInventoryVariantOptionDto).GetProperties()
            .Select(x => x.Name)
            .OrderBy(x => x)
            .ToArray();

        Assert.Equal(
            new[] { "RegionCode", "ServiceId", "ServiceName", "ServiceVariantId", "VariantName" },
            names);
    }

    [Fact]
    public void QueryRepository_UsesProjectionWithoutIncludeChains()
    {
        var path = Path.Combine(FindRepositoryRoot(), "ServiceBridge.Infrastructure", "Repositories", "AdminInventoryQueryRepository.cs");
        var source = File.ReadAllText(path);

        Assert.Contains(".Select(x => new AdminInventoryItemDto", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Include(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void VariantOptionsQuery_UsesBoundedSafeServerSideProjection()
    {
        var path = Path.Combine(FindRepositoryRoot(), "ServiceBridge.Infrastructure", "Repositories", "AdminInventoryQueryRepository.cs");
        var source = File.ReadAllText(path);

        Assert.Contains("x.Service.IsActive", source, StringComparison.Ordinal);
        Assert.Contains("x.IsActive", source, StringComparison.Ordinal);
        Assert.Contains("x.FulfillmentMode == FulfillmentMode.InventoryCode", source, StringComparison.Ordinal);
        Assert.Contains("x.Service.Name.Contains(search)", source, StringComparison.Ordinal);
        Assert.Contains("x.Name.Contains(search)", source, StringComparison.Ordinal);
        Assert.Contains("x.RegionCode.Contains(search)", source, StringComparison.Ordinal);
        Assert.Contains(".OrderBy(x => x.Service.Category.DisplayOrder)", source, StringComparison.Ordinal);
        Assert.Contains(".ThenBy(x => x.Service.Category.Name)", source, StringComparison.Ordinal);
        Assert.Contains(".ThenBy(x => x.Service.Name)", source, StringComparison.Ordinal);
        Assert.Contains(".ThenBy(x => x.DisplayOrder)", source, StringComparison.Ordinal);
        Assert.Contains(".ThenBy(x => x.Name)", source, StringComparison.Ordinal);
        Assert.Contains(".ThenBy(x => x.Id)", source, StringComparison.Ordinal);
        Assert.Contains(".Take(pageSize)", source, StringComparison.Ordinal);
        Assert.Contains(".Select(x => new AdminInventoryVariantOptionDto", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Include(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("InventoryItems", source[(source.IndexOf("GetVariantOptionsAsync", StringComparison.Ordinal))..], StringComparison.Ordinal);
    }

    [Fact]
    public void AdminInventoryEndpoints_RequireAdminRoleAndExposeNoRevealRoute()
    {
        var controller = typeof(AdminInventoryController);
        var authorization = controller.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single();
        var route = controller.GetCustomAttributes(typeof(RouteAttribute), true)
            .Cast<RouteAttribute>()
            .Single();
        var methodRoutes = controller.GetMethods()
            .SelectMany(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), true)
                .Cast<HttpMethodAttribute>())
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.Equal(AppConstants.Roles.Admin, authorization.Roles);
        Assert.Equal("api/admin/inventory", route.Template);
        Assert.Contains("batch", methodRoutes);
        Assert.Contains("variants", methodRoutes);
        Assert.Contains("summary/{serviceVariantId:guid}", methodRoutes);
        Assert.Contains("{inventoryItemId:guid}/disable", methodRoutes);
        Assert.DoesNotContain(methodRoutes, routeTemplate =>
            routeTemplate?.Contains("reveal", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void VariantOptionsEndpoint_IsAdminGetRoute()
    {
        var method = typeof(AdminInventoryController).GetMethod(
            nameof(AdminInventoryController.GetVariantOptions));
        var route = Assert.IsType<HttpGetAttribute>(
            method?.GetCustomAttributes(typeof(HttpGetAttribute), true).Single());

        Assert.Equal("variants", route.Template);
    }

    [Theory]
    [InlineData(AppConstants.Roles.Admin, true)]
    [InlineData(AppConstants.Roles.User, false)]
    [InlineData(AppConstants.Roles.Agent, false)]
    [InlineData(null, false)]
    public async Task AdminInventoryAuthorization_RequiresAuthenticatedAdmin(
        string? role,
        bool expectedSuccess)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        await using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var identity = role is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, role)],
                "InventoryAuthorizationTest");
        var principal = new ClaimsPrincipal(identity);
        var policy = new AuthorizationPolicyBuilder()
            .RequireRole(AppConstants.Roles.Admin)
            .Build();

        var result = await authorization.AuthorizeAsync(principal, policy);

        Assert.Equal(expectedSuccess, result.Succeeded);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ServiceBridge.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
