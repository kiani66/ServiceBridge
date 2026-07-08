using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Web.Pages.Orders;

[Authorize(Roles = AppConstants.Roles.User)]
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public PagedResultDto<UserOrderListItemDto> Orders { get; private set; } = new();
    public string? ErrorMessage { get; private set; }
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)]
    public OrderStatus? Status { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!Request.HasFormContentType)
        {
            var target = Status.HasValue
                ? $"/ui/orders?status={Uri.EscapeDataString(Status.Value.ToString())}"
                : "/ui/orders";

            return LocalRedirect(target);
        }

        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var query = $"api/orders/my?page={PageNumber}&pageSize=20";
            if (Status.HasValue)
            {
                query += $"&status={Status.Value}";
            }

            var response = await client.GetAsync(query);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var result = await response.Content.ReadFromJsonAsync<
                ServiceResult<PagedResultDto<UserOrderListItemDto>>>();

            if (result?.Success == true && result.Data != null)
            {
                Orders = result.Data;
                return Page();
            }

            ErrorMessage = result?.Message ?? "Unable to load your orders.";
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

    public string StatusCssClass(string status)
    {
        return status switch
        {
            "Completed" => "pb-status-success",
            "Cancelled" or "Failed" or "Refunded" => "pb-status-muted",
            "Processing" or "Assigned" => "pb-status-progress",
            _ => "pb-status-pending"
        };
    }

    public string StatusLabel(string status)
    {
        return status switch
        {
            "WaitingForPayment" => "Waiting for payment",
            _ => status
        };
    }

    private async Task<IActionResult> SignOutExpiredSessionAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["StatusMessage"] = AppMessages.SessionExpired;
        return RedirectToPage("/Account/Login");
    }
}
