using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Web.Pages.Admin.Catalog;

[Authorize(Roles = AppConstants.Roles.Admin)]
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IReadOnlyCollection<ServiceCategoryResponse> Categories { get; private set; } =
        Array.Empty<ServiceCategoryResponse>();
    public IReadOnlyCollection<AdminServiceSummaryResponse> Services { get; private set; } =
        Array.Empty<AdminServiceSummaryResponse>();
    public string? ErrorMessage { get; private set; }

    public Task<IActionResult> OnGetAsync()
    {
        return Task.FromResult<IActionResult>(LocalRedirect("/ui/admin/catalog"));
    }

    public async Task<IActionResult> OnPostCreateCategoryAsync(
        CreateServiceCategoryRequest category)
    {
        if (!TryValidateModel(category, nameof(category)))
        {
            ErrorMessage = "Review the category fields and try again.";
            return await LoadAsync();
        }

        return await PostAsync(
            "api/admin/catalog/categories",
            category,
            "Category created successfully.");
    }

    public async Task<IActionResult> OnPostCreateServiceAsync(
        CreateDigitalServiceRequest service)
    {
        if (!TryValidateModel(service, nameof(service)))
        {
            ErrorMessage = "Review the service fields and try again.";
            return await LoadAsync();
        }

        return await PostAsync(
            "api/admin/catalog/services",
            service,
            "Service created successfully.");
    }

    private async Task<IActionResult> PostAsync<TRequest>(
        string endpoint,
        TRequest request,
        string fallbackMessage)
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var response = await client.PostAsJsonAsync(endpoint, request);
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

            ErrorMessage = result?.Message ?? "Unable to save the catalog item.";
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
            var categoriesResponse =
                await client.GetAsync("api/admin/catalog/categories");
            if (categoriesResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var categoriesResult = await categoriesResponse.Content
                .ReadFromJsonAsync<
                    ServiceResult<IReadOnlyCollection<ServiceCategoryResponse>>>();
            if (categoriesResult?.Success == true &&
                categoriesResult.Data != null)
            {
                Categories = categoriesResult.Data;
            }
            else
            {
                ErrorMessage ??=
                    categoriesResult?.Message ?? "Unable to load categories.";
            }

            var servicesResponse =
                await client.GetAsync("api/admin/catalog/services");
            if (servicesResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var servicesResult = await servicesResponse.Content
                .ReadFromJsonAsync<
                    ServiceResult<IReadOnlyCollection<AdminServiceSummaryResponse>>>();
            if (servicesResult?.Success == true &&
                servicesResult.Data != null)
            {
                Services = servicesResult.Data;
            }
            else
            {
                ErrorMessage ??=
                    servicesResult?.Message ?? "Unable to load services.";
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

    private async Task<IActionResult> SignOutExpiredSessionAsync()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["StatusMessage"] = AppMessages.SessionExpired;
        return RedirectToPage("/Account/Login");
    }
}
