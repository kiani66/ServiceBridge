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

namespace ServiceBridge.Web.Pages;

[Authorize(Roles = AppConstants.Roles.Admin)]
public class DashboardModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DashboardModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public AdminOrderDashboardSummaryDto Summary { get; private set; } = new();
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var response = await client.GetAsync("api/admin/orders/summary");
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["StatusMessage"] = AppMessages.SessionExpired;
                return RedirectToPage("/Account/Login");
            }

            var result = await response.Content.ReadFromJsonAsync<
                ServiceResult<AdminOrderDashboardSummaryDto>>();
            if (result?.Success == true && result.Data != null)
            {
                Summary = result.Data;
            }
            else
            {
                ErrorMessage = result?.Message ?? "Unable to load the dashboard summary.";
            }
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
}
