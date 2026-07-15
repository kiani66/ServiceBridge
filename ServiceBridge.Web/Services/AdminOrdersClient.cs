using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class AdminOrdersClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AdminOrdersClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<AdminOrdersApiResult<AdminOrderDashboardSummaryDto>>
        GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync<AdminOrderDashboardSummaryDto>(
            client => client.GetAsync("api/admin/orders/summary", cancellationToken),
            "Unable to load the admin dashboard right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<PagedResultDto<AdminOrderListItemDto>>>
        GetOrdersAsync(
            int page,
            int pageSize,
            OrderStatus? status,
            string? search,
            Guid? assignedAgentId = null,
            CancellationToken cancellationToken = default)
    {
        var query = $"api/admin/orders?page={Math.Max(page, 1)}&pageSize={pageSize}";
        if (status.HasValue)
        {
            query += $"&status={Uri.EscapeDataString(status.Value.ToString())}";
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search.Trim())}";
        }

        if (assignedAgentId.HasValue)
        {
            query += $"&assignedAgentId={assignedAgentId.Value}";
        }

        return SendAsync<PagedResultDto<AdminOrderListItemDto>>(
            client => client.GetAsync(query, cancellationToken),
            "Unable to load orders right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<PagedResultDto<AdminAssignableOrderDto>>>
        GetPaidUnassignedOrdersAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        return SendAsync<PagedResultDto<AdminAssignableOrderDto>>(
            client => client.GetAsync(
                $"api/admin/orders/paid-unassigned?page={Math.Max(page, 1)}&pageSize={pageSize}",
                cancellationToken),
            "Unable to load the paid orders queue right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<PagedResultDto<AdminPendingInventoryFulfillmentDto>>>
        GetPendingInventoryFulfillmentAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        return SendAsync<PagedResultDto<AdminPendingInventoryFulfillmentDto>>(
            client => client.GetAsync(
                $"api/admin/orders/inventory-fulfillment/pending?page={Math.Max(page, 1)}&pageSize={pageSize}",
                cancellationToken),
            "Unable to load the inventory fulfillment queue right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<IReadOnlyCollection<AgentSummaryDto>>>
        GetActiveAgentsAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync<IReadOnlyCollection<AgentSummaryDto>>(
            client => client.GetAsync("api/admin/orders/agents", cancellationToken),
            "Unable to load active agents right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<AdminOrderDetailDto>> GetOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<AdminOrderDetailDto>(
            client => client.GetAsync($"api/admin/orders/{orderId}", cancellationToken),
            "Unable to load this order right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<AdminOrderCredentialsDto>>
        RevealCredentialsAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
    {
        return SendAsync<AdminOrderCredentialsDto>(
            client => client.PostAsync(
                $"api/admin/orders/{orderId}/credentials/reveal",
                content: null,
                cancellationToken),
            "Unable to reveal credentials right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<PagedResultDto<AdminPaymentListItemDto>>>
        GetPaymentsAsync(
            int page,
            int pageSize,
            PaymentStatus? status,
            string? search,
            CancellationToken cancellationToken = default)
    {
        var query = $"api/admin/orders/payments?page={Math.Max(page, 1)}&pageSize={pageSize}";
        if (status.HasValue)
        {
            query += $"&status={Uri.EscapeDataString(status.Value.ToString())}";
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search.Trim())}";
        }

        return SendAsync<PagedResultDto<AdminPaymentListItemDto>>(
            client => client.GetAsync(query, cancellationToken),
            "Unable to load payments right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<PagedResultDto<AdminAuditEventDto>>>
        GetAuditEventsAsync(
            AdminAuditQueryDto query,
            CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>
        {
            $"page={Math.Max(query.Page, 1)}",
            $"pageSize={query.PageSize}"
        };

        if (query.FromUtc.HasValue)
        {
            parameters.Add(
                $"fromUtc={Uri.EscapeDataString(query.FromUtc.Value.ToUniversalTime().ToString("O"))}");
        }

        if (query.ToUtc.HasValue)
        {
            parameters.Add(
                $"toUtc={Uri.EscapeDataString(query.ToUtc.Value.ToUniversalTime().ToString("O"))}");
        }

        AppendQuery(parameters, "actor", query.Actor);
        AppendQuery(parameters, "order", query.Order);
        AppendQuery(parameters, "eventType", query.EventType);

        if (query.OrderId.HasValue)
        {
            parameters.Add($"orderId={query.OrderId.Value}");
        }

        if (query.Status.HasValue)
        {
            parameters.Add(
                $"status={Uri.EscapeDataString(query.Status.Value.ToString())}");
        }

        return SendAsync<PagedResultDto<AdminAuditEventDto>>(
            client => client.GetAsync(
                $"api/admin/orders/audit?{string.Join("&", parameters)}",
                cancellationToken),
            "Unable to load audit events right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<IReadOnlyCollection<AdminOrderTimelineEventDto>>>
        GetOrderTimelineAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
    {
        return SendAsync<IReadOnlyCollection<AdminOrderTimelineEventDto>>(
            client => client.GetAsync(
                $"api/admin/orders/{orderId}/timeline",
                cancellationToken),
            "Unable to load the order timeline right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<bool>> AssignAsync(
        Guid orderId,
        Guid agentId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                $"api/admin/orders/{orderId}/assign",
                new AssignOrderRequest
                {
                    AgentId = agentId,
                    Notes = notes
                },
                cancellationToken),
            "Unable to assign this order right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<RetryInventoryCodeFulfillmentResultDto>>
        RetryInventoryCodeFulfillmentAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
    {
        return SendAsync<RetryInventoryCodeFulfillmentResultDto>(
            client => client.PostAsync(
                $"api/admin/orders/{orderId}/inventory-fulfillment/retry",
                content: null,
                cancellationToken),
            "Unable to retry inventory fulfillment right now.",
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
        catch (HttpRequestException)
        {
            return AdminOrdersApiResult<T>.Failed(
                "Unable to connect to the admin operations service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return AdminOrdersApiResult<T>.Failed(
                "The admin operations service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return AdminOrdersApiResult<T>.Failed(
                "The admin operations service returned an invalid response.");
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
                "Unable to connect to the admin operations service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return AdminOrdersApiResult<bool>.Failed(
                "The admin operations service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return AdminOrdersApiResult<bool>.Failed(
                "The admin operations service returned an invalid response.");
        }
    }

    private static string MessageFor(HttpStatusCode statusCode, string fallbackMessage)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "The requested order was not found.",
            HttpStatusCode.Conflict => "The order state changed. Refresh and try again.",
            HttpStatusCode.Forbidden => "You are not allowed to view admin operations.",
            _ => fallbackMessage
        };
    }

    private static void AppendQuery(
        List<string> parameters,
        string name,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parameters.Add(
                $"{name}={Uri.EscapeDataString(value.Trim())}");
        }
    }
}

public sealed record AdminOrdersApiResult<T>(
    bool Success,
    bool RequiresAuthentication,
    bool NotFound,
    bool Conflict,
    string? ErrorMessage,
    Dictionary<string, string[]>? ValidationErrors,
    T? Data)
{
    public static AdminOrdersApiResult<T> Succeeded(T data)
    {
        return new AdminOrdersApiResult<T>(
            true,
            false,
            false,
            false,
            null,
            null,
            data);
    }

    public static AdminOrdersApiResult<T> AuthRequired()
    {
        return new AdminOrdersApiResult<T>(
            false,
            true,
            false,
            false,
            "Sign in as an admin before viewing operations.",
            null,
            default);
    }

    public static AdminOrdersApiResult<T> Failed(
        string? errorMessage,
        Dictionary<string, string[]>? validationErrors = null,
        HttpStatusCode statusCode = 0)
    {
        return new AdminOrdersApiResult<T>(
            false,
            false,
            statusCode == HttpStatusCode.NotFound,
            statusCode == HttpStatusCode.Conflict,
            errorMessage,
            validationErrors,
            default);
    }
}
