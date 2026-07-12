using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Web.Services;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class AdminInventoryClientTests
{
    [Fact]
    public async Task GetVariantOptionsAsync_UsesSelectorRouteAndSerializesSearch()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return Task.FromResult(JsonResponse(ServiceResult<IReadOnlyList<AdminInventoryVariantOptionDto>>.Ok(
                [new AdminInventoryVariantOptionDto { ServiceName = "Music", VariantName = "Plus", RegionCode = "EU" }])));
        });
        var client = CreateClient(handler);

        var result = await client.GetVariantOptionsAsync("  music & plus  ", 75);

        Assert.True(result.Success);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("/api/admin/inventory/variants", capturedRequest.RequestUri!.AbsolutePath);
        var query = ParseQuery(capturedRequest.RequestUri.Query);
        Assert.Equal("music & plus", query["search"]);
        Assert.Equal("75", query["pageSize"]);
    }

    [Fact]
    public async Task GetInventoryItemsAsync_SerializesListFilters()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return Task.FromResult(JsonResponse(ServiceResult<PagedResultDto<AdminInventoryItemDto>>.Ok(
                new PagedResultDto<AdminInventoryItemDto>()
                {
                    Page = 2,
                    PageSize = 25,
                    TotalCount = 0
                })));
        });
        var client = CreateClient(handler);
        var variantId = Guid.NewGuid();

        var result = await client.GetInventoryItemsAsync(
            2,
            25,
            variantId,
            InventoryItemStatus.Available);

        Assert.True(result.Success);
        Assert.NotNull(capturedRequest);
        Assert.Equal("/api/admin/inventory", capturedRequest!.RequestUri!.AbsolutePath);
        var query = ParseQuery(capturedRequest.RequestUri.Query);
        Assert.Equal("2", query["pageNumber"]);
        Assert.Equal("25", query["pageSize"]);
        Assert.Equal(variantId.ToString(), query["serviceVariantId"]);
        Assert.Equal(nameof(InventoryItemStatus.Available), query["status"]);
    }

    [Fact]
    public async Task GetInventoryItemsAsync_OmitsNullableStatusWhenNotSelected()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return Task.FromResult(JsonResponse(ServiceResult<PagedResultDto<AdminInventoryItemDto>>.Ok(
                new PagedResultDto<AdminInventoryItemDto>())));
        });
        var client = CreateClient(handler);

        await client.GetInventoryItemsAsync(1, 25, Guid.NewGuid(), status: null);

        Assert.NotNull(capturedRequest);
        Assert.DoesNotContain("status=", capturedRequest!.RequestUri!.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAvailabilitySummaryAsync_UsesVariantSummaryRoute()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return Task.FromResult(JsonResponse(ServiceResult<InventoryAvailabilitySummaryDto>.Ok(
                new InventoryAvailabilitySummaryDto { Available = 3 })));
        });
        var client = CreateClient(handler);
        var variantId = Guid.NewGuid();

        var result = await client.GetAvailabilitySummaryAsync(variantId);

        Assert.True(result.Success);
        Assert.NotNull(capturedRequest);
        Assert.Equal($"/api/admin/inventory/summary/{variantId}", capturedRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CreateAndDisableAsync_UseExactPostRoutesAndSafeResultMapping()
    {
        var requests = new List<HttpRequestMessage>();
        string? batchBody = null;
        var variantId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var handler = new StubHttpMessageHandler(async request =>
        {
            requests.Add(request);
            if (request.RequestUri!.AbsolutePath.EndsWith("/batch", StringComparison.Ordinal))
            {
                batchBody = await request.Content!.ReadAsStringAsync();
                return JsonResponse(ServiceResult<CreateInventoryItemsResponse>.Created(
                    new CreateInventoryItemsResponse { ServiceVariantId = variantId, CreatedCount = 2 }));
            }

            return JsonResponse(ServiceResult.Ok());
        });
        var client = CreateClient(handler);

        var createResult = await client.CreateInventoryItemsAsync(new CreateInventoryItemsRequest
        {
            ServiceVariantId = variantId,
            Codes = ["CaseSensitive-A", "CaseSensitive-b"]
        });
        var disableResult = await client.DisableInventoryItemAsync(itemId);

        Assert.True(createResult.Success);
        Assert.Equal(2, createResult.Data?.CreatedCount);
        Assert.True(disableResult.Success);
        Assert.Equal(HttpMethod.Post, requests[0].Method);
        Assert.Equal("/api/admin/inventory/batch", requests[0].RequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Post, requests[1].Method);
        Assert.Equal($"/api/admin/inventory/{itemId}/disable", requests[1].RequestUri!.AbsolutePath);

        Assert.NotNull(batchBody);
        using var document = JsonDocument.Parse(batchBody!);
        var root = document.RootElement;
        Assert.Equal(variantId, root.GetProperty("serviceVariantId").GetGuid());
        Assert.Equal(
            new[] { "CaseSensitive-A", "CaseSensitive-b" },
            root.GetProperty("codes").EnumerateArray().Select(code => code.GetString()).ToArray());
        Assert.Equal(
            new[] { "codes", "serviceVariantId" },
            root.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray());
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, false, true, false)]
    [InlineData(HttpStatusCode.BadRequest, false, false, false)]
    [InlineData(HttpStatusCode.Forbidden, false, false, false)]
    [InlineData(HttpStatusCode.Conflict, false, false, true)]
    [InlineData(HttpStatusCode.InternalServerError, false, false, false)]
    public async Task Client_MapsAuthenticationAndBusinessFailures(
        HttpStatusCode statusCode,
        bool expectedSuccess,
        bool expectedAuthentication,
        bool expectedConflict)
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(JsonResponse(
            ServiceResult<InventoryAvailabilitySummaryDto>.Fail("Safe API message", "Test", (int)statusCode),
            statusCode)));
        var client = CreateClient(handler);

        var result = await client.GetAvailabilitySummaryAsync(Guid.NewGuid());

        Assert.Equal(expectedSuccess, result.Success);
        Assert.Equal(expectedAuthentication, result.RequiresAuthentication);
        Assert.Equal(expectedConflict, result.Conflict);
    }

    [Fact]
    public async Task Client_MapsMalformedResponsesToSafeError()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("not-json")
        }));
        var client = CreateClient(handler);

        var result = await client.GetAvailabilitySummaryAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("The inventory service returned an invalid response.", result.ErrorMessage);
    }

    [Fact]
    public async Task Client_MapsNotFoundToSafeResult()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(JsonResponse(
            ServiceResult<InventoryAvailabilitySummaryDto>.Fail("Ignored", "NotFound", 404),
            HttpStatusCode.NotFound)));
        var client = CreateClient(handler);

        var result = await client.GetAvailabilitySummaryAsync(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.True(result.NotFound);
        Assert.Equal("Ignored", result.ErrorMessage);
    }

    [Fact]
    public async Task Client_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromCanceled<HttpResponseMessage>(cancellation.Token));
        var client = CreateClient(handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetVariantOptionsAsync(null, cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task Client_MapsNonCallerCancellationToSafeTimeoutFailure()
    {
        using var timeoutCancellation = new CancellationTokenSource();
        timeoutCancellation.Cancel();
        var handler = new StubHttpMessageHandler(_ =>
            Task.FromCanceled<HttpResponseMessage>(timeoutCancellation.Token));
        var client = CreateClient(handler);

        var result = await client.GetVariantOptionsAsync(null);

        Assert.False(result.Success);
        Assert.Equal("The request timed out. Please try again.", result.ErrorMessage);
    }

    [Fact]
    public void InventoryClientAndDtoContracts_DoNotExposeSensitiveCodeValues()
    {
        var methodNames = typeof(AdminInventoryClient).GetMethods()
            .Select(method => method.Name)
            .ToArray();
        var optionProperties = typeof(AdminInventoryVariantOptionDto).GetProperties()
            .Select(property => property.Name)
            .ToArray();
        var itemProperties = typeof(AdminInventoryItemDto).GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain(methodNames, name => name.Contains("Reveal", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(optionProperties, name => name.Contains("ProtectedCode", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(optionProperties, name => name.Contains("CodeFingerprint", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(itemProperties, name => name.Contains("ProtectedCode", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(itemProperties, name => name.Contains("CodeFingerprint", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void InventoryPage_UsesRequiredSharedComponentsAndSafeActions()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "ServiceBridge.Web",
            "Components",
            "Pages",
            "AdminInventory.razor"));

        Assert.Contains("@page \"/ui/admin/inventory\"", source, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Roles = AppConstants.Roles.Admin)]", source, StringComparison.Ordinal);
        Assert.Contains("<ConfirmationDialog", source, StringComparison.Ordinal);
        Assert.Contains("<LoadingState", source, StringComparison.Ordinal);
        Assert.Contains("<ErrorState", source, StringComparison.Ordinal);
        Assert.Contains("<EmptyState", source, StringComparison.Ordinal);
        Assert.Contains("context.Status == InventoryItemStatus.Available", source, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@MutationBusy\"", source, StringComparison.Ordinal);
        Assert.Contains("var submittedVariantId = _selectedVariantId.Value", source, StringComparison.Ordinal);
        Assert.Contains("var originatingVariantId = _selectedVariantId.Value", source, StringComparison.Ordinal);
        Assert.Contains("IsCurrentVariantContext", source, StringComparison.Ordinal);
        Assert.Contains("IsBusy=\"_disableBusy\"", source, StringComparison.Ordinal);
        Assert.Contains("if (_disableBusy ||", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ProtectedCode", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CodeFingerprint", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Reveal", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConfirmationDialog_DisablesActionsWhileBusy()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "ServiceBridge.Web",
            "Components",
            "Shared",
            "ConfirmationDialog.razor"));

        Assert.Contains("[Parameter] public bool IsBusy { get; set; }", source, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@IsBusy\"", source, StringComparison.Ordinal);
        Assert.Contains("if (IsBusy)", source, StringComparison.Ordinal);
    }

    private static AdminInventoryClient CreateClient(HttpMessageHandler handler)
    {
        return new AdminInventoryClient(new StubHttpClientFactory(handler));
    }

    private static HttpResponseMessage JsonResponse<T>(T value, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(value)
        };
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        return query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                parts => Uri.UnescapeDataString(parts[0]),
                parts => parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : string.Empty);
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

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal(AppConstants.Api.HttpClientName, name);
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.test/")
            };
        }
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return responseFactory(request);
        }
    }
}
