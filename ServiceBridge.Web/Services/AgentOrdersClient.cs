using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class AgentOrdersClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AgentOrdersClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<AgentOrdersApiResult<AgentOrderDashboardSummaryDto>>
        GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync<AgentOrderDashboardSummaryDto>(
            client => client.GetAsync("api/agent/orders/dashboard", cancellationToken),
            "Unable to load the agent dashboard right now.",
            cancellationToken);
    }

    public Task<AgentOrdersApiResult<PagedResultDto<AgentOrderListItemDto>>>
        GetOrdersAsync(
            int page,
            int pageSize,
            OrderStatus? status,
            string? search,
            CancellationToken cancellationToken = default)
    {
        var query = $"api/agent/orders?page={Math.Max(page, 1)}&pageSize={pageSize}";
        if (status.HasValue)
        {
            query += $"&status={Uri.EscapeDataString(status.Value.ToString())}";
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search.Trim())}";
        }

        return SendAsync<PagedResultDto<AgentOrderListItemDto>>(
            client => client.GetAsync(query, cancellationToken),
            "Unable to load assigned orders right now.",
            cancellationToken);
    }

    public Task<AgentOrdersApiResult<AgentOrderDetailDto>> GetOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<AgentOrderDetailDto>(
            client => client.GetAsync($"api/agent/orders/{orderId}", cancellationToken),
            "Unable to load this assigned order right now.",
            cancellationToken);
    }

    public Task<AgentOrdersApiResult<bool>> StartProcessingAsync(
        Guid orderId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                $"api/agent/orders/{orderId}/start",
                new AgentOrderTransitionRequest { Note = note },
                cancellationToken),
            "Unable to start processing right now.",
            cancellationToken);
    }

    public Task<AgentOrdersApiResult<bool>> CompleteAsync(
        Guid orderId,
        string? note,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                $"api/agent/orders/{orderId}/complete",
                new AgentOrderTransitionRequest { Note = note },
                cancellationToken),
            "Unable to complete this order right now.",
            cancellationToken);
    }

    public Task<AgentOrdersApiResult<bool>> RequestCustomerActionAsync(
        Guid orderId,
        string note,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                $"api/agent/orders/{orderId}/request-customer-action",
                new RequestCustomerActionRequest { Note = note },
                cancellationToken),
            "Unable to request customer information right now.",
            cancellationToken);
    }

    public Task<AgentOrdersApiResult<bool>> FailAsync(
        Guid orderId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                $"api/agent/orders/{orderId}/fail",
                new FailOrderRequest { Reason = reason },
                cancellationToken),
            "Unable to fail this order right now.",
            cancellationToken);
    }

    public Task<AgentOrdersApiResult<bool>> SaveDeliveryCredentialsAsync(
        Guid orderId,
        IReadOnlyDictionary<string, string> credentials,
        CancellationToken cancellationToken = default)
    {
        var request = new SaveAgentDeliveryCredentialsRequest
        {
            Credentials = credentials
                .Select(item => new CreateOrderFieldRequest
                {
                    FieldKey = item.Key,
                    Value = item.Value.Trim()
                })
                .ToList()
        };

        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                $"api/agent/orders/{orderId}/delivery-credentials",
                request,
                cancellationToken),
            "Unable to save delivery credentials right now.",
            cancellationToken);
    }

    public Task<AgentOrdersApiResult<bool>> AddNoteAsync(
        Guid orderId,
        string text,
        bool isVisibleToCustomer,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                $"api/agent/orders/{orderId}/notes",
                new AddAgentOrderNoteRequest
                {
                    Text = text,
                    IsVisibleToCustomer = isVisibleToCustomer
                },
                cancellationToken),
            "Unable to add this note right now.",
            cancellationToken);
    }

    private async Task<AgentOrdersApiResult<T>> SendAsync<T>(
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
                return AgentOrdersApiResult<T>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<T>>(cancellationToken);

            if (result?.Success == true && result.Data is not null)
            {
                return AgentOrdersApiResult<T>.Succeeded(result.Data);
            }

            return AgentOrdersApiResult<T>.Failed(
                result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                result?.ValidationErrors,
                response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return AgentOrdersApiResult<T>.Failed(
                "Unable to connect to the agent order service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return AgentOrdersApiResult<T>.Failed(
                "The agent order service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return AgentOrdersApiResult<T>.Failed(
                "The agent order service returned an invalid response.");
        }
    }

    private async Task<AgentOrdersApiResult<bool>> SendCommandAsync(
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
                return AgentOrdersApiResult<bool>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult>(cancellationToken);

            return result?.Success == true
                ? AgentOrdersApiResult<bool>.Succeeded(true)
                : AgentOrdersApiResult<bool>.Failed(
                    result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                    result?.ValidationErrors,
                    response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return AgentOrdersApiResult<bool>.Failed(
                "Unable to connect to the agent order service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return AgentOrdersApiResult<bool>.Failed(
                "The agent order service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return AgentOrdersApiResult<bool>.Failed(
                "The agent order service returned an invalid response.");
        }
    }

    private static string MessageFor(HttpStatusCode statusCode, string fallbackMessage)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "The requested assigned order was not found.",
            HttpStatusCode.Conflict => "The order state changed. Refresh and try again.",
            HttpStatusCode.Forbidden => "You are not allowed to view this order.",
            _ => fallbackMessage
        };
    }
}

public sealed record AgentOrdersApiResult<T>(
    bool Success,
    bool RequiresAuthentication,
    bool NotFound,
    bool Conflict,
    string? ErrorMessage,
    Dictionary<string, string[]>? ValidationErrors,
    T? Data)
{
    public static AgentOrdersApiResult<T> Succeeded(T data)
    {
        return new AgentOrdersApiResult<T>(
            true,
            false,
            false,
            false,
            null,
            null,
            data);
    }

    public static AgentOrdersApiResult<T> AuthRequired()
    {
        return new AgentOrdersApiResult<T>(
            false,
            true,
            false,
            false,
            "Sign in as an agent before viewing assigned orders.",
            null,
            default);
    }

    public static AgentOrdersApiResult<T> Failed(
        string? errorMessage,
        Dictionary<string, string[]>? validationErrors = null,
        HttpStatusCode statusCode = 0)
    {
        return new AgentOrdersApiResult<T>(
            false,
            false,
            statusCode == HttpStatusCode.NotFound,
            statusCode == HttpStatusCode.Conflict,
            errorMessage,
            validationErrors,
            default);
    }
}
