using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class AdminCatalogClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AdminCatalogClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AdminOrdersApiResult<AdminCatalogSnapshot>> GetCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await SendAsync<IReadOnlyCollection<ServiceCategoryResponse>>(
            client => client.GetAsync("api/admin/catalog/categories", cancellationToken),
            "Unable to load categories.",
            cancellationToken);
        if (!categories.Success)
        {
            return AdminOrdersApiResult<AdminCatalogSnapshot>.Failed(
                categories.ErrorMessage,
                categories.ValidationErrors);
        }

        var services = await SendAsync<IReadOnlyCollection<AdminServiceSummaryResponse>>(
            client => client.GetAsync("api/admin/catalog/services", cancellationToken),
            "Unable to load services.",
            cancellationToken);
        if (!services.Success)
        {
            return AdminOrdersApiResult<AdminCatalogSnapshot>.Failed(
                services.ErrorMessage,
                services.ValidationErrors);
        }

        return AdminOrdersApiResult<AdminCatalogSnapshot>.Succeeded(
            new AdminCatalogSnapshot(
                categories.Data ?? Array.Empty<ServiceCategoryResponse>(),
                services.Data ?? Array.Empty<AdminServiceSummaryResponse>()));
    }

    public Task<AdminOrdersApiResult<AdminServiceEditorSnapshot>> GetServiceAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        return SendServiceSnapshotAsync(serviceId, cancellationToken);
    }

    public Task<AdminOrdersApiResult<bool>> CreateCategoryAsync(
        CreateServiceCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                "api/admin/catalog/categories",
                request,
                cancellationToken),
            "Unable to create the category.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<bool>> CreateServiceAsync(
        CreateDigitalServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                "api/admin/catalog/services",
                request,
                cancellationToken),
            "Unable to create the service.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<bool>> UpdateServiceAsync(
        Guid serviceId,
        UpdateDigitalServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PutAsJsonAsync(
                $"api/admin/catalog/services/{serviceId}",
                request,
                cancellationToken),
            "Unable to update the service.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<bool>> CreateVariantAsync(
        Guid serviceId,
        CreateServiceVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                $"api/admin/catalog/services/{serviceId}/plans",
                request,
                cancellationToken),
            "Unable to create the service variant.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<bool>> UpdateVariantAsync(
        Guid variantId,
        UpdateServiceVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PutAsJsonAsync(
                $"api/admin/catalog/plans/{variantId}",
                request,
                cancellationToken),
            "Unable to update the service variant.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<bool>> CreateFieldAsync(
        Guid serviceId,
        CreateServiceRequiredFieldRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                $"api/admin/catalog/services/{serviceId}/fields",
                request,
                cancellationToken),
            "Unable to create the required field.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<bool>> UpdateFieldAsync(
        Guid fieldId,
        UpdateServiceRequiredFieldRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PutAsJsonAsync(
                $"api/admin/catalog/fields/{fieldId}",
                request,
                cancellationToken),
            "Unable to update the required field.",
            cancellationToken);
    }

    private async Task<AdminOrdersApiResult<AdminServiceEditorSnapshot>>
        SendServiceSnapshotAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var service = await SendAsync<AdminServiceResponse>(
            client => client.GetAsync(
                $"api/admin/catalog/services/{serviceId}",
                cancellationToken),
            "Unable to load the service.",
            cancellationToken);
        if (!service.Success)
        {
            return AdminOrdersApiResult<AdminServiceEditorSnapshot>.Failed(
                service.ErrorMessage,
                service.ValidationErrors,
                service.NotFound ? HttpStatusCode.NotFound : 0);
        }

        var categories = await SendAsync<IReadOnlyCollection<ServiceCategoryResponse>>(
            client => client.GetAsync("api/admin/catalog/categories", cancellationToken),
            "Unable to load categories.",
            cancellationToken);
        if (!categories.Success)
        {
            return AdminOrdersApiResult<AdminServiceEditorSnapshot>.Failed(
                categories.ErrorMessage,
                categories.ValidationErrors);
        }

        return AdminOrdersApiResult<AdminServiceEditorSnapshot>.Succeeded(
            new AdminServiceEditorSnapshot(
                service.Data!,
                categories.Data ?? Array.Empty<ServiceCategoryResponse>()));
    }

    private async Task<AdminOrdersApiResult<T>> SendAsync<T>(
        Func<HttpClient, Task<HttpResponseMessage>> send,
        string fallbackMessage,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            using var response = await send(client);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return AdminOrdersApiResult<T>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<T>>(cancellationToken);

            if (result?.Success == true && result.Data is not null)
            {
                return AdminOrdersApiResult<T>.Succeeded(result.Data);
            }

            return AdminOrdersApiResult<T>.Failed(
                result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                result?.ValidationErrors,
                response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return AdminOrdersApiResult<T>.Failed(
                "Unable to connect to the admin catalog service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return AdminOrdersApiResult<T>.Failed(
                "The admin catalog service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return AdminOrdersApiResult<T>.Failed(
                "The admin catalog service returned an invalid response.");
        }
    }

    private async Task<AdminOrdersApiResult<bool>> SendCommandAsync(
        Func<HttpClient, Task<HttpResponseMessage>> send,
        string fallbackMessage,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            using var response = await send(client);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return AdminOrdersApiResult<bool>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult>(cancellationToken);

            return result?.Success == true
                ? AdminOrdersApiResult<bool>.Succeeded(true)
                : AdminOrdersApiResult<bool>.Failed(
                    result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                    result?.ValidationErrors,
                    response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return AdminOrdersApiResult<bool>.Failed(
                "Unable to connect to the admin catalog service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return AdminOrdersApiResult<bool>.Failed(
                "The admin catalog service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return AdminOrdersApiResult<bool>.Failed(
                "The admin catalog service returned an invalid response.");
        }
    }

    private static string MessageFor(HttpStatusCode statusCode, string fallbackMessage)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "The requested catalog item was not found.",
            HttpStatusCode.Conflict => "The catalog item changed. Refresh and try again.",
            HttpStatusCode.Forbidden => "You are not allowed to manage the catalog.",
            _ => fallbackMessage
        };
    }
}

public sealed record AdminCatalogSnapshot(
    IReadOnlyCollection<ServiceCategoryResponse> Categories,
    IReadOnlyCollection<AdminServiceSummaryResponse> Services);

public sealed record AdminServiceEditorSnapshot(
    AdminServiceResponse Service,
    IReadOnlyCollection<ServiceCategoryResponse> Categories);
