using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Web.Pages.Admin.Pricing;

[Authorize(Roles = AppConstants.Roles.Admin)]
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IReadOnlyCollection<ExchangeRateResponse> ExchangeRates { get; private set; } =
        Array.Empty<ExchangeRateResponse>();
    public IReadOnlyCollection<ProfitMarginResponse> ProfitMargins { get; private set; } =
        Array.Empty<ProfitMarginResponse>();
    public IReadOnlyCollection<ServiceCategoryResponse> Categories { get; private set; } =
        Array.Empty<ServiceCategoryResponse>();
    public IReadOnlyCollection<AdminServiceSummaryResponse> Services { get; private set; } =
        Array.Empty<AdminServiceSummaryResponse>();
    public string? ErrorMessage { get; private set; }
    public string DefaultEffectiveUtc =>
        DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm");

    public Task<IActionResult> OnGetAsync()
    {
        return Task.FromResult<IActionResult>(LocalRedirect("/ui/admin/pricing"));
    }

    public async Task<IActionResult> OnPostCreateExchangeRateAsync(
        string currencyCode,
        decimal rateToIrr,
        DateTime effectiveFromUtc,
        bool isActive)
    {
        var request = new CreateExchangeRateRequest
        {
            CurrencyCode = currencyCode,
            RateToIrr = rateToIrr,
            EffectiveFromUtc = DateTime.SpecifyKind(
                effectiveFromUtc,
                DateTimeKind.Utc),
            IsActive = isActive
        };

        return await SendAsync(
            HttpMethod.Post,
            "api/admin/pricing/exchange-rates",
            request,
            "Exchange rate created successfully.");
    }

    public async Task<IActionResult> OnPostCreateProfitMarginAsync(
        string scope,
        Guid? targetId,
        decimal percentage,
        DateTime effectiveFromUtc,
        bool isActive)
    {
        var request = new CreateProfitMarginRequest
        {
            Percentage = percentage,
            EffectiveFromUtc = DateTime.SpecifyKind(
                effectiveFromUtc,
                DateTimeKind.Utc),
            IsActive = isActive
        };

        switch (scope)
        {
            case "Category" when targetId.HasValue:
                request.CategoryId = targetId.Value;
                break;
            case "Service" when targetId.HasValue:
                request.ServiceId = targetId.Value;
                break;
            case "Global":
                break;
            default:
                ErrorMessage = "Select a valid margin scope and target.";
                return await LoadAsync();
        }

        return await SendAsync(
            HttpMethod.Post,
            "api/admin/pricing/profit-margins",
            request,
            "Profit margin created successfully.");
    }

    public Task<IActionResult> OnPostSetExchangeRateStatusAsync(
        Guid id,
        bool isActive)
    {
        return SendAsync(
            HttpMethod.Patch,
            $"api/admin/pricing/exchange-rates/{id}/status",
            new UpdatePricingStatusRequest { IsActive = isActive },
            "Exchange rate status updated.");
    }

    public Task<IActionResult> OnPostSetProfitMarginStatusAsync(
        Guid id,
        bool isActive)
    {
        return SendAsync(
            HttpMethod.Patch,
            $"api/admin/pricing/profit-margins/{id}/status",
            new UpdatePricingStatusRequest { IsActive = isActive },
            "Profit margin status updated.");
    }

    public string MarginTargetLabel(ProfitMarginResponse margin)
    {
        if (margin.ServiceId.HasValue)
        {
            return Services
                       .FirstOrDefault(x => x.Id == margin.ServiceId.Value)
                       ?.Name ??
                   margin.ServiceId.Value.ToString();
        }

        if (margin.CategoryId.HasValue)
        {
            return Categories
                       .FirstOrDefault(x => x.Id == margin.CategoryId.Value)
                       ?.Name ??
                   margin.CategoryId.Value.ToString();
        }

        return "All services";
    }

    private async Task<IActionResult> SendAsync<TRequest>(
        HttpMethod method,
        string endpoint,
        TRequest request,
        string fallbackMessage)
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            using var message = new HttpRequestMessage(method, endpoint)
            {
                Content = JsonContent.Create(request)
            };
            var response = await client.SendAsync(message);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var result = await response.Content.ReadFromJsonAsync<ServiceResult>();
            if (result?.Success == true)
            {
                TempData["StatusMessage"] = result.Message ?? fallbackMessage;
                return RedirectToPage();
            }

            ErrorMessage = result?.Message ?? "Unable to update pricing.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage =
                "Unable to connect to the server. Please try again later.";
        }
        catch (NotSupportedException)
        {
            ErrorMessage = "The server returned an unsupported response.";
        }
        catch (System.Text.Json.JsonException)
        {
            ErrorMessage = "The server returned an invalid response.";
        }

        return await LoadAsync();
    }

    private async Task<IActionResult> LoadAsync()
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            if (!await LoadExchangeRatesAsync(client) ||
                !await LoadProfitMarginsAsync(client) ||
                !await LoadCategoriesAsync(client) ||
                !await LoadServicesAsync(client))
            {
                ErrorMessage ??= "Unable to load pricing configuration.";
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage ??=
                "Unable to connect to the server. Please try again later.";
        }
        catch (NotSupportedException)
        {
            ErrorMessage ??= "The server returned an unsupported response.";
        }
        catch (System.Text.Json.JsonException)
        {
            ErrorMessage ??= "The server returned an invalid response.";
        }

        return Page();
    }

    private async Task<bool> LoadExchangeRatesAsync(HttpClient client)
    {
        var result = await client.GetFromJsonAsync<
            ServiceResult<IReadOnlyCollection<ExchangeRateResponse>>>(
            "api/admin/pricing/exchange-rates");
        if (result?.Success != true || result.Data == null)
        {
            ErrorMessage ??= result?.Message;
            return false;
        }

        ExchangeRates = result.Data;
        return true;
    }

    private async Task<bool> LoadProfitMarginsAsync(HttpClient client)
    {
        var result = await client.GetFromJsonAsync<
            ServiceResult<IReadOnlyCollection<ProfitMarginResponse>>>(
            "api/admin/pricing/profit-margins");
        if (result?.Success != true || result.Data == null)
        {
            ErrorMessage ??= result?.Message;
            return false;
        }

        ProfitMargins = result.Data;
        return true;
    }

    private async Task<bool> LoadCategoriesAsync(HttpClient client)
    {
        var result = await client.GetFromJsonAsync<
            ServiceResult<IReadOnlyCollection<ServiceCategoryResponse>>>(
            "api/admin/catalog/categories");
        if (result?.Success != true || result.Data == null)
        {
            ErrorMessage ??= result?.Message;
            return false;
        }

        Categories = result.Data;
        return true;
    }

    private async Task<bool> LoadServicesAsync(HttpClient client)
    {
        var result = await client.GetFromJsonAsync<
            ServiceResult<IReadOnlyCollection<AdminServiceSummaryResponse>>>(
            "api/admin/catalog/services");
        if (result?.Success != true || result.Data == null)
        {
            ErrorMessage ??= result?.Message;
            return false;
        }

        Services = result.Data;
        return true;
    }

    private async Task<IActionResult> SignOutExpiredSessionAsync()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["StatusMessage"] = AppMessages.SessionExpired;
        return RedirectToPage("/Account/Login");
    }
}
