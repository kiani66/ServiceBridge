using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class AdminInventoryClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AdminInventoryClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<AdminOrdersApiResult<IReadOnlyList<AdminInventoryVariantOptionDto>>>
        GetVariantOptionsAsync(
            string? search,
            int pageSize = 100,
            CancellationToken cancellationToken = default)
    {
        var query = $"api/admin/inventory/variants?pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search.Trim())}";
        }

        return SendAsync<IReadOnlyList<AdminInventoryVariantOptionDto>>(
            client => client.GetAsync(query, cancellationToken),
            "Unable to load inventory variants right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<PagedResultDto<AdminInventoryItemDto>>>
        GetInventoryItemsAsync(
            int pageNumber,
            int pageSize,
            Guid? serviceVariantId,
            InventoryItemStatus? status,
            CancellationToken cancellationToken = default)
    {
        var query = $"api/admin/inventory?pageNumber={Math.Max(pageNumber, 1)}&pageSize={pageSize}";
        if (serviceVariantId.HasValue)
        {
            query += $"&serviceVariantId={serviceVariantId.Value}";
        }

        if (status.HasValue)
        {
            query += $"&status={Uri.EscapeDataString(status.Value.ToString())}";
        }

        return SendAsync<PagedResultDto<AdminInventoryItemDto>>(
            client => client.GetAsync(query, cancellationToken),
            "Unable to load inventory items right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<InventoryAvailabilitySummaryDto>>
        GetAvailabilitySummaryAsync(
            Guid serviceVariantId,
            CancellationToken cancellationToken = default)
    {
        return SendAsync<InventoryAvailabilitySummaryDto>(
            client => client.GetAsync(
                $"api/admin/inventory/summary/{serviceVariantId}",
                cancellationToken),
            "Unable to load inventory availability right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<CreateInventoryItemsResponse>>
        CreateInventoryItemsAsync(
            CreateInventoryItemsRequest request,
            CancellationToken cancellationToken = default)
    {
        return SendAsync<CreateInventoryItemsResponse>(
            client => client.PostAsJsonAsync(
                "api/admin/inventory/batch",
                request,
                cancellationToken),
            "Unable to import inventory codes right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<bool>> DisableInventoryItemAsync(
        Guid inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsync(
                $"api/admin/inventory/{inventoryItemId}/disable",
                content: null,
                cancellationToken),
            "Unable to disable this inventory item right now.",
            cancellationToken);
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return AdminOrdersApiResult<T>.Failed("The request timed out. Please try again.");
        }
        catch (HttpRequestException)
        {
            return AdminOrdersApiResult<T>.Failed(
                "Unable to connect to the inventory service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return AdminOrdersApiResult<T>.Failed(
                "The inventory service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return AdminOrdersApiResult<T>.Failed(
                "The inventory service returned an invalid response.");
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return AdminOrdersApiResult<bool>.Failed("The request timed out. Please try again.");
        }
        catch (HttpRequestException)
        {
            return AdminOrdersApiResult<bool>.Failed(
                "Unable to connect to the inventory service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return AdminOrdersApiResult<bool>.Failed(
                "The inventory service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return AdminOrdersApiResult<bool>.Failed(
                "The inventory service returned an invalid response.");
        }
    }

    private static string MessageFor(HttpStatusCode statusCode, string fallbackMessage)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "The requested inventory record was not found.",
            HttpStatusCode.Conflict => "The inventory item changed. Refresh and try again.",
            HttpStatusCode.Forbidden => "You are not allowed to manage inventory.",
            _ => fallbackMessage
        };
    }
}
