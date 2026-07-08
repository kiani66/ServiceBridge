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
public class ServiceModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ServiceModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public AdminServiceResponse? Service { get; private set; }
    public IReadOnlyCollection<ServiceCategoryResponse> Categories { get; private set; } =
        Array.Empty<ServiceCategoryResponse>();
    public string? ErrorMessage { get; private set; }

    public Task<IActionResult> OnGetAsync(Guid id)
    {
        var target = id == Guid.Empty
            ? "/ui/admin/catalog"
            : $"/ui/admin/catalog/services/{id}";
        return Task.FromResult<IActionResult>(LocalRedirect(target));
    }

    public async Task<IActionResult> OnPostUpdateServiceAsync(
        Guid id,
        UpdateDigitalServiceRequest service)
    {
        if (!TryValidateModel(service, nameof(service)))
        {
            ErrorMessage = "Review the service fields and try again.";
            return await LoadAsync(id);
        }

        return await PutAsync(
            id,
            $"api/admin/catalog/services/{id}",
            service,
            "Service updated successfully.");
    }

    public async Task<IActionResult> OnPostCreateVariantAsync(
        Guid id,
        CreateServiceVariantRequest variant)
    {
        if (!TryValidateModel(variant, nameof(variant)))
        {
            ErrorMessage = "Review the service variant and try again.";
            return await LoadAsync(id);
        }

        return await PostAsync(
            id,
            $"api/admin/catalog/services/{id}/plans",
            variant,
            "service variant created successfully.");
    }

    public async Task<IActionResult> OnPostUpdateVariantAsync(
        Guid id,
        Guid variantId,
        UpdateServiceVariantRequest variant)
    {
        if (variantId == Guid.Empty ||
            !TryValidateModel(variant, nameof(variant)))
        {
            ErrorMessage = "Review the service variant and try again.";
            return await LoadAsync(id);
        }

        return await PutAsync(
            id,
            $"api/admin/catalog/plans/{variantId}",
            variant,
            "service variant updated successfully.");
    }

    public async Task<IActionResult> OnPostCreateFieldAsync(
        Guid id,
        CreateServiceRequiredFieldRequest field)
    {
        if (!TryValidateModel(field, nameof(field)))
        {
            ErrorMessage = "Review the required field and try again.";
            return await LoadAsync(id);
        }

        return await PostAsync(
            id,
            $"api/admin/catalog/services/{id}/fields",
            field,
            "Required field created successfully.");
    }

    public async Task<IActionResult> OnPostUpdateFieldAsync(
        Guid id,
        Guid fieldId,
        UpdateServiceRequiredFieldRequest field)
    {
        if (fieldId == Guid.Empty ||
            !TryValidateModel(field, nameof(field)))
        {
            ErrorMessage = "Review the required field and try again.";
            return await LoadAsync(id);
        }

        return await PutAsync(
            id,
            $"api/admin/catalog/fields/{fieldId}",
            field,
            "Required field updated successfully.");
    }

    private Task<IActionResult> PostAsync<TRequest>(
        Guid id,
        string endpoint,
        TRequest request,
        string fallbackMessage)
    {
        return SendAsync(
            id,
            endpoint,
            request,
            fallbackMessage,
            static (client, url, body) => client.PostAsJsonAsync(url, body));
    }

    private Task<IActionResult> PutAsync<TRequest>(
        Guid id,
        string endpoint,
        TRequest request,
        string fallbackMessage)
    {
        return SendAsync(
            id,
            endpoint,
            request,
            fallbackMessage,
            static (client, url, body) => client.PutAsJsonAsync(url, body));
    }

    private async Task<IActionResult> SendAsync<TRequest>(
        Guid id,
        string endpoint,
        TRequest request,
        string fallbackMessage,
        Func<HttpClient, string, TRequest, Task<HttpResponseMessage>> send)
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var response = await send(client, endpoint, request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var result = await response.Content.ReadFromJsonAsync<ServiceResult>();
            if (result?.Success == true)
            {
                TempData["StatusMessage"] = result.Message ?? fallbackMessage;
                return RedirectToPage(new { id });
            }

            ErrorMessage = result?.Message ?? "Unable to update the service.";
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

        return await LoadAsync(id);
    }

    private async Task<IActionResult> LoadAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return RedirectToPage("/Admin/Catalog/Index");
        }

        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var serviceResponse =
                await client.GetAsync($"api/admin/catalog/services/{id}");
            if (serviceResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            if (serviceResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            var serviceResult = await serviceResponse.Content
                .ReadFromJsonAsync<ServiceResult<AdminServiceResponse>>();
            if (serviceResult?.Success == true && serviceResult.Data != null)
            {
                Service = serviceResult.Data;
            }
            else
            {
                ErrorMessage ??=
                    serviceResult?.Message ?? "Unable to load the service.";
            }

            var categoriesResponse =
                await client.GetAsync("api/admin/catalog/categories");
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
