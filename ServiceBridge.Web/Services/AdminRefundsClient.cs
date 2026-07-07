using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class AdminRefundsClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AdminRefundsClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<AdminRefundsApiResult<PagedResultDto<AdminRefundRequestDto>>>
        GetRefundsAsync(
            int page,
            int pageSize,
            RefundStatus? status,
            CancellationToken cancellationToken = default)
    {
        var query = $"api/admin/refunds?page={Math.Max(page, 1)}&pageSize={pageSize}";
        if (status.HasValue)
        {
            query += $"&status={Uri.EscapeDataString(status.Value.ToString())}";
        }

        return SendAsync<PagedResultDto<AdminRefundRequestDto>>(
            client => client.GetAsync(query, cancellationToken),
            "Unable to load refund requests right now.",
            cancellationToken);
    }

    public Task<AdminRefundsApiResult<bool>> ApproveAsync(
        Guid refundRequestId,
        string? adminNote,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                $"api/admin/refunds/{refundRequestId}/approve",
                new ReviewRefundRequestRequest { AdminNote = adminNote },
                cancellationToken),
            "Unable to approve this refund request right now.",
            cancellationToken);
    }

    public Task<AdminRefundsApiResult<bool>> RejectAsync(
        Guid refundRequestId,
        string? adminNote,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                $"api/admin/refunds/{refundRequestId}/reject",
                new ReviewRefundRequestRequest { AdminNote = adminNote },
                cancellationToken),
            "Unable to reject this refund request right now.",
            cancellationToken);
    }

    private async Task<AdminRefundsApiResult<T>> SendAsync<T>(
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
                return AdminRefundsApiResult<T>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<T>>(cancellationToken);

            if (result?.Success == true && result.Data is not null)
            {
                return AdminRefundsApiResult<T>.Succeeded(result.Data);
            }

            return AdminRefundsApiResult<T>.Failed(
                result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                result?.ValidationErrors,
                response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return AdminRefundsApiResult<T>.Failed(
                "Unable to connect to the refund service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return AdminRefundsApiResult<T>.Failed(
                "The refund service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return AdminRefundsApiResult<T>.Failed(
                "The refund service returned an invalid response.");
        }
    }

    private async Task<AdminRefundsApiResult<bool>> SendCommandAsync(
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
                return AdminRefundsApiResult<bool>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult>(cancellationToken);

            return result?.Success == true
                ? AdminRefundsApiResult<bool>.Succeeded(true)
                : AdminRefundsApiResult<bool>.Failed(
                    result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                    result?.ValidationErrors,
                    response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return AdminRefundsApiResult<bool>.Failed(
                "Unable to connect to the refund service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return AdminRefundsApiResult<bool>.Failed(
                "The refund service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return AdminRefundsApiResult<bool>.Failed(
                "The refund service returned an invalid response.");
        }
    }

    private static string MessageFor(HttpStatusCode statusCode, string fallbackMessage)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "The requested refund request was not found.",
            HttpStatusCode.Conflict => "The refund request state changed. Refresh and try again.",
            HttpStatusCode.Forbidden => "You are not allowed to manage refunds.",
            _ => fallbackMessage
        };
    }
}

public sealed record AdminRefundsApiResult<T>(
    bool Success,
    bool RequiresAuthentication,
    bool Conflict,
    string? ErrorMessage,
    Dictionary<string, string[]>? ValidationErrors,
    T? Data)
{
    public static AdminRefundsApiResult<T> Succeeded(T data)
    {
        return new AdminRefundsApiResult<T>(
            true,
            false,
            false,
            null,
            null,
            data);
    }

    public static AdminRefundsApiResult<T> AuthRequired()
    {
        return new AdminRefundsApiResult<T>(
            false,
            true,
            false,
            "Sign in as an admin before managing refunds.",
            null,
            default);
    }

    public static AdminRefundsApiResult<T> Failed(
        string? errorMessage,
        Dictionary<string, string[]>? validationErrors = null,
        HttpStatusCode statusCode = 0)
    {
        return new AdminRefundsApiResult<T>(
            false,
            false,
            statusCode == HttpStatusCode.Conflict,
            errorMessage,
            validationErrors,
            default);
    }
}
