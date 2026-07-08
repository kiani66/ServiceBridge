using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class UserOrdersClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UserOrdersClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<UserOrdersApiResult<UserDashboardSummary>> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var ordersResult = await GetOrdersAsync(1, 100, null, cancellationToken);
        if (!ordersResult.Success)
        {
            return UserOrdersApiResult<UserDashboardSummary>.FromFailure(ordersResult);
        }

        var pendingResult = await GetOrdersAsync(
            1,
            1,
            OrderStatus.WaitingForPayment,
            cancellationToken);
        var paidResult = await GetOrdersAsync(
            1,
            1,
            OrderStatus.Paid,
            cancellationToken);
        var assignedResult = await GetOrdersAsync(
            1,
            1,
            OrderStatus.Assigned,
            cancellationToken);
        var processingResult = await GetOrdersAsync(
            1,
            1,
            OrderStatus.Processing,
            cancellationToken);
        var completedResult = await GetOrdersAsync(
            1,
            1,
            OrderStatus.Completed,
            cancellationToken);
        var failedResult = await GetOrdersAsync(
            1,
            1,
            OrderStatus.Failed,
            cancellationToken);

        var firstFailure = new[]
            {
                pendingResult,
                paidResult,
                assignedResult,
                processingResult,
                completedResult,
                failedResult
            }
            .FirstOrDefault(result => !result.Success);

        if (firstFailure is not null)
        {
            return UserOrdersApiResult<UserDashboardSummary>.FromFailure(firstFailure);
        }

        var activeOrders = ordersResult.Data!.Items
            .Where(order => IsActiveOrder(order.Status))
            .OrderByDescending(order => order.CreatedAt)
            .Take(8)
            .ToArray();

        var totalPaymentsIrr = ordersResult.Data.Items
            .Where(order => order.PaymentStatus == nameof(PaymentStatus.Completed))
            .Sum(order => order.FinalPriceIrr);

        return UserOrdersApiResult<UserDashboardSummary>.Succeeded(
            new UserDashboardSummary(
                ordersResult.Data.TotalCount,
                pendingResult.Data!.TotalCount,
                paidResult.Data!.TotalCount,
                assignedResult.Data!.TotalCount,
                processingResult.Data!.TotalCount,
                completedResult.Data!.TotalCount,
                failedResult.Data!.TotalCount,
                totalPaymentsIrr,
                ordersResult.Data.TotalCount > ordersResult.Data.Items.Count,
                activeOrders,
                ordersResult.Data.Items.Take(5).ToArray()));
    }

    public Task<UserOrdersApiResult<PagedResultDto<UserOrderListItemDto>>> GetOrdersAsync(
        int page,
        int pageSize,
        OrderStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = $"api/orders/my?page={Math.Max(page, 1)}&pageSize={pageSize}";
        if (status.HasValue)
        {
            query += $"&status={Uri.EscapeDataString(status.Value.ToString())}";
        }

        return SendAsync<PagedResultDto<UserOrderListItemDto>>(
            client => client.GetAsync(query, cancellationToken),
            "Unable to load your orders right now.",
            cancellationToken);
    }

    public Task<UserOrdersApiResult<UserOrderDetailDto>> GetOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<UserOrderDetailDto>(
            client => client.GetAsync($"api/orders/my/{orderId}", cancellationToken),
            "Unable to load this order right now.",
            cancellationToken);
    }

    public Task<UserOrdersApiResult<bool>> RespondToCustomerActionAsync(
        Guid orderId,
        Guid requestId,
        string responseMessage,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                $"api/orders/my/{orderId}/customer-actions/respond",
                new RespondToCustomerActionRequest
                {
                    RequestId = requestId,
                    Response = responseMessage
                },
                cancellationToken),
            "Unable to submit your response right now.",
            cancellationToken);
    }

    private async Task<UserOrdersApiResult<T>> SendAsync<T>(
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
                return UserOrdersApiResult<T>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<T>>(cancellationToken);

            if (result?.Success == true && result.Data is not null)
            {
                return UserOrdersApiResult<T>.Succeeded(result.Data);
            }

            return UserOrdersApiResult<T>.Failed(
                result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                result?.ValidationErrors,
                response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return UserOrdersApiResult<T>.Failed(
                "Unable to connect to the order service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return UserOrdersApiResult<T>.Failed(
                "The order service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return UserOrdersApiResult<T>.Failed(
                "The order service returned an invalid response.");
        }
    }

    private async Task<UserOrdersApiResult<bool>> SendCommandAsync(
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
                return UserOrdersApiResult<bool>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult>(cancellationToken);

            return result?.Success == true
                ? UserOrdersApiResult<bool>.Succeeded(true)
                : UserOrdersApiResult<bool>.Failed(
                    result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                    result?.ValidationErrors,
                    response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return UserOrdersApiResult<bool>.Failed(
                "Unable to connect to the order service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return UserOrdersApiResult<bool>.Failed(
                "The order service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return UserOrdersApiResult<bool>.Failed(
                "The order service returned an invalid response.");
        }
    }

    private static string MessageFor(HttpStatusCode statusCode, string fallbackMessage)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "The requested order was not found.",
            HttpStatusCode.Forbidden => "You are not allowed to view this order.",
            _ => fallbackMessage
        };
    }

    private static bool IsActiveOrder(string status)
    {
        return status is nameof(OrderStatus.WaitingForPayment) or
            nameof(OrderStatus.Paid) or
            nameof(OrderStatus.Assigned) or
            nameof(OrderStatus.Processing);
    }
}

public sealed record UserDashboardSummary(
    int TotalOrders,
    int PendingOrders,
    int PaidOrders,
    int AssignedOrders,
    int ProcessingOrders,
    int CompletedOrders,
    int FailedOrders,
    decimal TotalPaymentsIrr,
    bool IsPaymentTotalPartial,
    IReadOnlyCollection<UserOrderListItemDto> ActiveOrders,
    IReadOnlyCollection<UserOrderListItemDto> RecentOrders);

public sealed record UserOrdersApiResult<T>(
    bool Success,
    bool RequiresAuthentication,
    bool NotFound,
    string? ErrorMessage,
    Dictionary<string, string[]>? ValidationErrors,
    T? Data)
{
    public static UserOrdersApiResult<T> Succeeded(T data)
    {
        return new UserOrdersApiResult<T>(
            true,
            false,
            false,
            null,
            null,
            data);
    }

    public static UserOrdersApiResult<T> AuthRequired()
    {
        return new UserOrdersApiResult<T>(
            false,
            true,
            false,
            "Sign in before viewing orders.",
            null,
            default);
    }

    public static UserOrdersApiResult<T> Failed(
        string? errorMessage,
        Dictionary<string, string[]>? validationErrors = null,
        HttpStatusCode statusCode = 0)
    {
        return new UserOrdersApiResult<T>(
            false,
            false,
            statusCode == HttpStatusCode.NotFound,
            errorMessage,
            validationErrors,
            default);
    }

    public static UserOrdersApiResult<T> FromFailure<TSource>(
        UserOrdersApiResult<TSource> result)
    {
        return new UserOrdersApiResult<T>(
            false,
            result.RequiresAuthentication,
            result.NotFound,
            result.ErrorMessage,
            result.ValidationErrors,
            default);
    }
}
