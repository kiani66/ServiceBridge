using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class PaymentClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PaymentClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<PaymentApiResult<UserOrderDetailDto>> GetOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<UserOrderDetailDto>(
            client => client.GetAsync($"api/orders/my/{orderId}", cancellationToken),
            "Unable to load the order right now.",
            cancellationToken);
    }

    public Task<PaymentApiResult<PaymentInitiatedResponse>> InitiatePaymentAsync(
        Guid orderId,
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<PaymentInitiatedResponse>(
            client => client.PostAsJsonAsync(
                "api/payments/initiate",
                new InitiatePaymentRequest
                {
                    OrderId = orderId,
                    PaymentId = paymentId
                },
                cancellationToken),
            "Unable to start the payment right now.",
            cancellationToken);
    }

    public Task<PaymentApiResult<MockPaymentResultResponse>> ProcessMockPaymentAsync(
        Guid paymentId,
        bool succeed,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<MockPaymentResultResponse>(
            client => client.PostAsJsonAsync(
                "api/payments/mock",
                new MockPaymentRequest
                {
                    PaymentId = paymentId,
                    Succeed = succeed
                },
                cancellationToken),
            "Unable to process the mock payment right now.",
            cancellationToken);
    }

    private async Task<PaymentApiResult<T>> SendAsync<T>(
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
                return PaymentApiResult<T>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<T>>(cancellationToken);

            if (result?.Success == true && result.Data is not null)
            {
                return PaymentApiResult<T>.Succeeded(result.Data);
            }

            return PaymentApiResult<T>.Failed(
                result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                result?.ValidationErrors,
                response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return PaymentApiResult<T>.Failed(
                "Unable to connect to the payment service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return PaymentApiResult<T>.Failed(
                "The payment service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return PaymentApiResult<T>.Failed(
                "The payment service returned an invalid response.");
        }
    }

    private static string MessageFor(HttpStatusCode statusCode, string fallbackMessage)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "The requested payment or order was not found.",
            HttpStatusCode.Conflict => "The payment state changed. Refresh and try again.",
            HttpStatusCode.Forbidden => "You are not allowed to process this payment.",
            _ => fallbackMessage
        };
    }
}

public sealed record PaymentApiResult<T>(
    bool Success,
    bool RequiresAuthentication,
    bool NotFound,
    bool Conflict,
    string? ErrorMessage,
    Dictionary<string, string[]>? ValidationErrors,
    T? Data)
{
    public static PaymentApiResult<T> Succeeded(T data)
    {
        return new PaymentApiResult<T>(
            true,
            false,
            false,
            false,
            null,
            null,
            data);
    }

    public static PaymentApiResult<T> AuthRequired()
    {
        return new PaymentApiResult<T>(
            false,
            true,
            false,
            false,
            "Sign in before continuing payment.",
            null,
            default);
    }

    public static PaymentApiResult<T> Failed(
        string? errorMessage,
        Dictionary<string, string[]>? validationErrors = null,
        HttpStatusCode statusCode = 0)
    {
        return new PaymentApiResult<T>(
            false,
            false,
            statusCode == HttpStatusCode.NotFound,
            statusCode == HttpStatusCode.Conflict,
            errorMessage,
            validationErrors,
            default);
    }
}
