using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class AdminPricingClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AdminPricingClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AdminOrdersApiResult<AdminPricingSnapshot>> GetPricingAsync(
        CancellationToken cancellationToken = default)
    {
        var exchangeRates = await SendAsync<IReadOnlyCollection<ExchangeRateResponse>>(
            client => client.GetAsync("api/admin/pricing/exchange-rates", cancellationToken),
            "Unable to load exchange rates.",
            cancellationToken);
        if (!exchangeRates.Success)
        {
            return FailedSnapshot(exchangeRates);
        }

        var profitMargins = await SendAsync<IReadOnlyCollection<ProfitMarginResponse>>(
            client => client.GetAsync("api/admin/pricing/profit-margins", cancellationToken),
            "Unable to load profit margins.",
            cancellationToken);
        if (!profitMargins.Success)
        {
            return FailedSnapshot(profitMargins);
        }

        var categories = await SendAsync<IReadOnlyCollection<ServiceCategoryResponse>>(
            client => client.GetAsync("api/admin/catalog/categories", cancellationToken),
            "Unable to load categories.",
            cancellationToken);
        if (!categories.Success)
        {
            return FailedSnapshot(categories);
        }

        var services = await SendAsync<IReadOnlyCollection<AdminServiceSummaryResponse>>(
            client => client.GetAsync("api/admin/catalog/services", cancellationToken),
            "Unable to load services.",
            cancellationToken);
        if (!services.Success)
        {
            return FailedSnapshot(services);
        }

        return AdminOrdersApiResult<AdminPricingSnapshot>.Succeeded(
            new AdminPricingSnapshot(
                exchangeRates.Data ?? Array.Empty<ExchangeRateResponse>(),
                profitMargins.Data ?? Array.Empty<ProfitMarginResponse>(),
                categories.Data ?? Array.Empty<ServiceCategoryResponse>(),
                services.Data ?? Array.Empty<AdminServiceSummaryResponse>()));
    }

    public Task<AdminOrdersApiResult<bool>> CreateExchangeRateAsync(
        CreateExchangeRateRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                "api/admin/pricing/exchange-rates",
                request,
                cancellationToken),
            "Unable to create the exchange rate.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<bool>> CreateProfitMarginAsync(
        CreateProfitMarginRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                "api/admin/pricing/profit-margins",
                request,
                cancellationToken),
            "Unable to create the profit margin.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<bool>> SetExchangeRateStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PatchAsJsonAsync(
                $"api/admin/pricing/exchange-rates/{id}/status",
                new UpdatePricingStatusRequest { IsActive = isActive },
                cancellationToken),
            "Unable to update the exchange rate.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<bool>> SetProfitMarginStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PatchAsJsonAsync(
                $"api/admin/pricing/profit-margins/{id}/status",
                new UpdatePricingStatusRequest { IsActive = isActive },
                cancellationToken),
            "Unable to update the profit margin.",
            cancellationToken);
    }

    private static AdminOrdersApiResult<AdminPricingSnapshot> FailedSnapshot<T>(
        AdminOrdersApiResult<T> result)
    {
        return AdminOrdersApiResult<AdminPricingSnapshot>.Failed(
            result.ErrorMessage,
            result.ValidationErrors);
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
                "Unable to connect to the admin pricing service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return AdminOrdersApiResult<T>.Failed(
                "The admin pricing service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return AdminOrdersApiResult<T>.Failed(
                "The admin pricing service returned an invalid response.");
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
                "Unable to connect to the admin pricing service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return AdminOrdersApiResult<bool>.Failed(
                "The admin pricing service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return AdminOrdersApiResult<bool>.Failed(
                "The admin pricing service returned an invalid response.");
        }
    }

    private static string MessageFor(HttpStatusCode statusCode, string fallbackMessage)
    {
        return statusCode switch
        {
            HttpStatusCode.Conflict => "The pricing record changed. Refresh and try again.",
            HttpStatusCode.Forbidden => "You are not allowed to manage pricing.",
            _ => fallbackMessage
        };
    }
}

public sealed record AdminPricingSnapshot(
    IReadOnlyCollection<ExchangeRateResponse> ExchangeRates,
    IReadOnlyCollection<ProfitMarginResponse> ProfitMargins,
    IReadOnlyCollection<ServiceCategoryResponse> Categories,
    IReadOnlyCollection<AdminServiceSummaryResponse> Services);
