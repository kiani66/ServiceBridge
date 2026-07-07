using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class RefundsClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public RefundsClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<RefundsApiResult<PagedResultDto<RefundRequestDto>>> GetOrderRefundsAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<PagedResultDto<RefundRequestDto>>(
            client => client.GetAsync(
                $"api/orders/my/{orderId}/refunds",
                cancellationToken),
            "Unable to load refund requests right now.",
            cancellationToken);
    }

    public Task<RefundsApiResult<RefundRequestDto>> CreateAsync(
        Guid orderId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<RefundRequestDto>(
            client => client.PostAsJsonAsync(
                $"api/orders/my/{orderId}/refunds",
                new CreateRefundRequestRequest { Reason = reason },
                cancellationToken),
            "Unable to submit this refund request right now.",
            cancellationToken);
    }

    private async Task<RefundsApiResult<T>> SendAsync<T>(
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
                return RefundsApiResult<T>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<T>>(cancellationToken);

            if (result?.Success == true && result.Data is not null)
            {
                return RefundsApiResult<T>.Succeeded(result.Data);
            }

            return RefundsApiResult<T>.Failed(
                result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                result?.ValidationErrors,
                response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return RefundsApiResult<T>.Failed(
                "Unable to connect to the refund service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return RefundsApiResult<T>.Failed(
                "The refund service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return RefundsApiResult<T>.Failed(
                "The refund service returned an invalid response.");
        }
    }

    private static string MessageFor(HttpStatusCode statusCode, string fallbackMessage)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "The requested refund resource was not found.",
            HttpStatusCode.Conflict => "The refund request state changed. Refresh and try again.",
            _ => fallbackMessage
        };
    }
}

public sealed record RefundsApiResult<T>(
    bool Success,
    bool RequiresAuthentication,
    bool Conflict,
    string? ErrorMessage,
    Dictionary<string, string[]>? ValidationErrors,
    T? Data)
{
    public static RefundsApiResult<T> Succeeded(T data)
    {
        return new RefundsApiResult<T>(
            true,
            false,
            false,
            null,
            null,
            data);
    }

    public static RefundsApiResult<T> AuthRequired()
    {
        return new RefundsApiResult<T>(
            false,
            true,
            false,
            "Sign in before managing refunds.",
            null,
            default);
    }

    public static RefundsApiResult<T> Failed(
        string? errorMessage,
        Dictionary<string, string[]>? validationErrors = null,
        HttpStatusCode statusCode = 0)
    {
        return new RefundsApiResult<T>(
            false,
            false,
            statusCode == HttpStatusCode.Conflict,
            errorMessage,
            validationErrors,
            default);
    }
}
