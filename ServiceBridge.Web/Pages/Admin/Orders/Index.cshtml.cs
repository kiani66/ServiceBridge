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

namespace ServiceBridge.Web.Pages.Admin.Orders;

[Authorize(Roles = AppConstants.Roles.Admin)]
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public PagedResultDto<AdminAssignableOrderDto> Orders { get; private set; } =
        new();
    public IReadOnlyCollection<AgentSummaryDto> Agents { get; private set; } =
        Array.Empty<AgentSummaryDto>();
    public string? ErrorMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public Task<IActionResult> OnGetAsync()
    {
        return LoadAsync();
    }

    public async Task<IActionResult> OnPostAssignAsync(
        Guid orderId,
        Guid agentId,
        string? notes)
    {
        if (orderId == Guid.Empty || agentId == Guid.Empty)
        {
            ErrorMessage = "Select an agent before assigning the order.";
            return await LoadAsync();
        }

        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var response = await client.PostAsJsonAsync(
                $"api/admin/orders/{orderId}/assign",
                new AssignOrderRequest
                {
                    AgentId = agentId,
                    Notes = notes
                });
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var result = await response.Content.ReadFromJsonAsync<ServiceResult>();
            if (result?.Success == true)
            {
                TempData["StatusMessage"] =
                    result.Message ?? "Order assigned successfully.";
                return RedirectToPage(new { pageNumber = PageNumber });
            }

            ErrorMessage = result?.Message ?? "Unable to assign the order.";
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

        return await LoadAsync();
    }

    private async Task<IActionResult> LoadAsync()
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var ordersResponse = await client.GetAsync(
                $"api/admin/orders/paid-unassigned?page={PageNumber}&pageSize=20");
            if (ordersResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var ordersResult = await ordersResponse.Content.ReadFromJsonAsync<
                ServiceResult<PagedResultDto<AdminAssignableOrderDto>>>();
            if (ordersResult?.Success == true && ordersResult.Data != null)
            {
                Orders = ordersResult.Data;
            }
            else
            {
                ErrorMessage ??= ordersResult?.Message ??
                    "Unable to load the assignment queue.";
            }

            var agentsResponse = await client.GetAsync("api/admin/orders/agents");
            if (agentsResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var agentsResult = await agentsResponse.Content.ReadFromJsonAsync<
                ServiceResult<IReadOnlyCollection<AgentSummaryDto>>>();
            if (agentsResult?.Success == true && agentsResult.Data != null)
            {
                Agents = agentsResult.Data;
            }
            else
            {
                ErrorMessage ??= agentsResult?.Message ??
                    "Unable to load active agents.";
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
