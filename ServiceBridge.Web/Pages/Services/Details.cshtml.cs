using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Web.Pages.Services;

public class DetailsModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DetailsModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public ServiceDetailResponse? Service { get; private set; }
    public PricingQuoteResponse? Quote { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? PricingMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid? SelectedServiceVariantId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? SelectedPlanId { get; set; }

    [BindProperty]
    public OrderFulfillmentMode SelectedFulfillmentMode { get; set; } =
        OrderFulfillmentMode.UseExistingAccount;

    [BindProperty]
    public List<OrderFieldInput> OrderFields { get; set; } = new();

    public IReadOnlyCollection<ServiceRequiredFieldResponse> CustomerOrderFields =>
        Service?.RequiredFields
            .Where(x =>
                x.FulfillmentMode == SelectedFulfillmentMode &&
                x.Actor == CredentialActor.Customer)
            .OrderBy(x => x.DisplayOrder)
            .ToArray() ?? Array.Empty<ServiceRequiredFieldResponse>();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return RedirectToPage("/Services/Index");
        }

        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var response = await client.GetAsync($"api/services/{id}");
            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<ServiceDetailResponse>>();

            if (result?.Success == true && result.Data != null)
            {
                Service = result.Data;
                SelectedServiceVariantId = GetDefaultServiceVariantId();
                SelectedPlanId = SelectedServiceVariantId;
                SelectedFulfillmentMode = GetDefaultFulfillmentMode();
                InitializeOrderFields();
                await LoadQuoteAsync(client, id, SelectedServiceVariantId);
                return Page();
            }

            ErrorMessage = result?.Message ?? "Unable to load this service.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to connect to the server. Please try again later.";
        }
        catch (NotSupportedException)
        {
            ErrorMessage = "The server returned an unsupported response.";
        }
        catch (System.Text.Json.JsonException)
        {
            ErrorMessage = "The server returned an invalid response.";
        }

        return Page();
    }

    public async Task<IActionResult> OnGetQuoteAsync(
        Guid id,
        Guid? serviceVariantId,
        Guid? planId)
    {
        if (id == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = "Service is required."
            });
        }

        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);
        var result = await ReadQuoteAsync(client, id, serviceVariantId ?? planId);
        return new JsonResult(new
        {
            success = result.Quote != null,
            message = result.Message,
            quote = result.Quote
        });
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return RedirectToPage("/Services/Index");
        }

        if (User.Identity?.IsAuthenticated != true ||
            !User.IsInRole(AppConstants.Roles.User))
        {
            return Forbid();
        }

        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var request = new CreateOrderRequest
            {
                ServiceId = id,
                ServiceVariantId = SelectedServiceVariantId ?? SelectedPlanId,
                OrderType = OrderType.Purchase,
                FulfillmentMode = SelectedFulfillmentMode,
                Credentials = OrderFields.Select(x => new CreateOrderFieldRequest
                {
                    FieldKey = x.FieldKey,
                    Value = x.Value?.Trim() ?? string.Empty
                }).ToList()
            };

            var response = await client.PostAsJsonAsync("api/orders", request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<OrderCreatedResponse>>();
            if (result?.Success == true && result.Data != null)
            {
                TempData["StatusMessage"] =
                    result.Message ?? $"Order {result.Data.OrderNumber} created successfully.";
                return RedirectToPage("/Orders/Details", new { id = result.Data.OrderId });
            }

            AddValidationErrors(result?.ValidationErrors);
            ErrorMessage = result?.Message ?? response.StatusCode switch
            {
                HttpStatusCode.NotFound => "The selected service is no longer available.",
                HttpStatusCode.Conflict => "The order could not be created due to a conflict. Please try again.",
                _ => "Unable to create the order."
            };
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to connect to the server. Please try again later.";
        }
        catch (NotSupportedException)
        {
            ErrorMessage = "The server returned an unsupported response.";
        }
        catch (System.Text.Json.JsonException)
        {
            ErrorMessage = "The server returned an invalid response.";
        }

        await LoadServiceForPostAsync(client, id);
        return Page();
    }

    private async Task LoadQuoteAsync(
        HttpClient client,
        Guid serviceId,
        Guid? serviceVariantId)
    {
        try
        {
            var result = await ReadQuoteAsync(client, serviceId, serviceVariantId);
            if (result.Quote != null)
            {
                Quote = result.Quote;
                return;
            }

            PricingMessage = result.Message ?? "Local pricing is currently unavailable.";
        }
        catch (HttpRequestException)
        {
            PricingMessage = "Local pricing is currently unavailable.";
        }
        catch (NotSupportedException)
        {
            PricingMessage = "Local pricing is currently unavailable.";
        }
        catch (System.Text.Json.JsonException)
        {
            PricingMessage = "Local pricing is currently unavailable.";
        }
    }

    private static async Task<(PricingQuoteResponse? Quote, string? Message)> ReadQuoteAsync(
        HttpClient client,
        Guid serviceId,
        Guid? serviceVariantId)
    {
        var quoteUrl = serviceVariantId.HasValue
            ? $"api/services/{serviceId}/quote?serviceVariantId={serviceVariantId.Value}"
            : $"api/services/{serviceId}/quote";
        var response = await client.GetAsync(quoteUrl);
        var result = await response.Content
            .ReadFromJsonAsync<ServiceResult<PricingQuoteResponse>>();

        return result?.Success == true && result.Data != null
            ? (result.Data, result.Message)
            : (null, result?.Message);
    }

    private async Task LoadServiceForPostAsync(HttpClient client, Guid id)
    {
        try
        {
            var response = await client.GetAsync($"api/services/{id}");
            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<ServiceDetailResponse>>();
            if (result?.Success == true && result.Data != null)
            {
                Service = result.Data;
                SelectedServiceVariantId = GetDefaultServiceVariantId();
                SelectedPlanId = SelectedServiceVariantId;
                MergeOrderFields();
                await LoadQuoteAsync(client, id, SelectedServiceVariantId);
                return;
            }

            ErrorMessage ??= result?.Message ?? "Unable to reload this service.";
        }
        catch (HttpRequestException)
        {
            ErrorMessage ??= "Unable to connect to the server. Please try again later.";
        }
        catch (NotSupportedException)
        {
            ErrorMessage ??= "The server returned an unsupported response.";
        }
        catch (System.Text.Json.JsonException)
        {
            ErrorMessage ??= "The server returned an invalid response.";
        }
    }

    private void InitializeOrderFields()
    {
        OrderFields = CustomerOrderFields
            .Select(x => new OrderFieldInput { FieldKey = x.FieldKey })
            .ToList();
    }

    private void MergeOrderFields()
    {
        if (Service == null)
        {
            return;
        }

        var submitted = OrderFields.ToDictionary(
            x => x.FieldKey,
            StringComparer.OrdinalIgnoreCase);
        OrderFields = CustomerOrderFields
            .Select(x => submitted.TryGetValue(x.FieldKey, out var input)
                ? input
                : new OrderFieldInput { FieldKey = x.FieldKey })
            .ToList();
    }

    private OrderFulfillmentMode GetDefaultFulfillmentMode()
    {
        if (Service?.SupportedFulfillmentModes.Contains(SelectedFulfillmentMode) == true)
        {
            return SelectedFulfillmentMode;
        }

        return Service?.SupportedFulfillmentModes.FirstOrDefault() ??
            OrderFulfillmentMode.UseExistingAccount;
    }

    private Guid? GetDefaultServiceVariantId()
    {
        if (Service?.Plans.Count > 0 != true)
        {
            return null;
        }

        var selectedVariantId = SelectedServiceVariantId ?? SelectedPlanId;
        if (selectedVariantId.HasValue &&
            Service.Plans.Any(x => x.Id == selectedVariantId.Value))
        {
            return selectedVariantId;
        }

        return Service.Plans
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .First()
            .Id;
    }

    private void AddValidationErrors(Dictionary<string, string[]>? validationErrors)
    {
        if (validationErrors == null)
        {
            return;
        }

        foreach (var (key, errors) in validationErrors)
        {
            var index = OrderFields.FindIndex(x =>
                string.Equals(x.FieldKey, key, StringComparison.OrdinalIgnoreCase));
            var modelKey = index >= 0 ? $"{nameof(OrderFields)}[{index}].Value" : string.Empty;
            foreach (var error in errors)
            {
                ModelState.AddModelError(modelKey, error);
            }
        }
    }

    private async Task<IActionResult> SignOutExpiredSessionAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["StatusMessage"] = AppMessages.SessionExpired;
        return RedirectToPage("/Account/Login");
    }

    public sealed class OrderFieldInput
    {
        public string FieldKey { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
