using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class CheckoutClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CheckoutClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<CheckoutCreateOrderResult> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            using var response = await client.PostAsJsonAsync(
                "api/orders",
                request,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return CheckoutCreateOrderResult.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<OrderCreatedResponse>>(
                    cancellationToken);

            if (result?.Success == true && result.Data is not null)
            {
                return CheckoutCreateOrderResult.Created(result.Data);
            }

            return CheckoutCreateOrderResult.Failed(
                result?.Message ?? response.StatusCode switch
                {
                    HttpStatusCode.NotFound => "The selected service is no longer available.",
                    HttpStatusCode.Conflict => "The order could not be created. Please try again.",
                    _ => "Unable to create the order right now."
                },
                result?.ValidationErrors);
        }
        catch (HttpRequestException)
        {
            return CheckoutCreateOrderResult.Failed(
                "Unable to connect to the order service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return CheckoutCreateOrderResult.Failed(
                "The order service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return CheckoutCreateOrderResult.Failed(
                "The order service returned an invalid response.");
        }
    }
}

public sealed record CheckoutCreateOrderResult(
    bool Success,
    bool RequiresAuthentication,
    string? ErrorMessage,
    Dictionary<string, string[]>? ValidationErrors,
    OrderCreatedResponse? Order)
{
    public static CheckoutCreateOrderResult Created(OrderCreatedResponse order)
    {
        return new CheckoutCreateOrderResult(
            true,
            false,
            null,
            null,
            order);
    }

    public static CheckoutCreateOrderResult AuthRequired()
    {
        return new CheckoutCreateOrderResult(
            false,
            true,
            "Sign in before creating an order.",
            null,
            null);
    }

    public static CheckoutCreateOrderResult Failed(
        string? errorMessage,
        Dictionary<string, string[]>? validationErrors = null)
    {
        return new CheckoutCreateOrderResult(
            false,
            false,
            errorMessage,
            validationErrors,
            null);
    }
}
