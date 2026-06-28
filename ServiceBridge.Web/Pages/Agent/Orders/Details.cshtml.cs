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

namespace ServiceBridge.Web.Pages.Agent.Orders;

[Authorize(Roles = AppConstants.Roles.Agent)]
public class DetailsModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DetailsModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public AgentOrderDetailDto? Order { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty]
    public string? ActionNote { get; set; }

    [BindProperty]
    public string CustomerActionNote { get; set; } = string.Empty;

    [BindProperty]
    public string NoteText { get; set; } = string.Empty;

    [BindProperty]
    public bool NoteVisibleToCustomer { get; set; }

    [BindProperty]
    public string FailureReason { get; set; } = string.Empty;

    [BindProperty]
    public List<DeliveryCredentialInput> DeliveryCredentials { get; set; } = new();

    public Task<IActionResult> OnGetAsync(Guid id)
    {
        return LoadOrderAsync(id);
    }

    public Task<IActionResult> OnPostStartAsync(Guid id)
    {
        return PostActionAsync(
            id,
            "start",
            new AgentOrderTransitionRequest { Note = ActionNote });
    }

    public Task<IActionResult> OnPostResumeAsync(Guid id)
    {
        return PostActionAsync(
            id,
            "resume",
            new AgentOrderTransitionRequest { Note = ActionNote });
    }

    public Task<IActionResult> OnPostCompleteAsync(Guid id)
    {
        return PostActionAsync(
            id,
            "complete",
            new AgentOrderTransitionRequest { Note = ActionNote });
    }

    public async Task<IActionResult> OnPostSaveDeliveryCredentialsAsync(Guid id)
    {
        var request = new SaveAgentDeliveryCredentialsRequest
        {
            Credentials = DeliveryCredentials.Select(x => new CreateOrderFieldRequest
            {
                FieldKey = x.FieldKey,
                Value = x.Value?.Trim() ?? string.Empty
            }).ToList()
        };

        return await PostActionAsync(
            id,
            "delivery-credentials",
            request);
    }

    public Task<IActionResult> OnPostFailAsync(Guid id)
    {
        if (string.IsNullOrWhiteSpace(FailureReason))
        {
            ErrorMessage = "Enter the reason the order could not be completed.";
            return LoadOrderAsync(id);
        }

        return PostActionAsync(
            id,
            "fail",
            new FailOrderRequest { Reason = FailureReason });
    }

    public Task<IActionResult> OnPostRequestCustomerActionAsync(Guid id)
    {
        if (string.IsNullOrWhiteSpace(CustomerActionNote))
        {
            ErrorMessage = "Describe the action required from the customer.";
            return LoadOrderAsync(id);
        }

        return PostActionAsync(
            id,
            "request-customer-action",
            new RequestCustomerActionRequest { Note = CustomerActionNote });
    }

    public Task<IActionResult> OnPostAddNoteAsync(Guid id)
    {
        if (string.IsNullOrWhiteSpace(NoteText))
        {
            ErrorMessage = "Enter a note before saving.";
            return LoadOrderAsync(id);
        }

        return PostActionAsync(
            id,
            "notes",
            new AddAgentOrderNoteRequest
            {
                Text = NoteText,
                IsVisibleToCustomer = NoteVisibleToCustomer
            });
    }

    public string StatusCssClass(string status)
    {
        return status switch
        {
            "Completed" => "pb-status-success",
            "Failed" or "Cancelled" => "pb-status-muted",
            _ => "pb-status-progress"
        };
    }

    public string StatusLabel(string status)
    {
        return status;
    }

    private async Task<IActionResult> PostActionAsync<TRequest>(
        Guid id,
        string action,
        TRequest request)
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var response = await client.PostAsJsonAsync(
                $"api/agent/orders/{id}/{action}",
                request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var result = await response.Content.ReadFromJsonAsync<ServiceResult>();
            if (result?.Success == true)
            {
                TempData["StatusMessage"] =
                    result.Message ?? "Order updated successfully.";
                return RedirectToPage(new { id });
            }

            ErrorMessage = result?.Message ?? "Unable to update the order.";
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

        return await LoadOrderAsync(id);
    }

    private async Task<IActionResult> LoadOrderAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return RedirectToPage("/Agent/Orders/Index");
        }

        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var response = await client.GetAsync($"api/agent/orders/{id}");
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            var result = await response.Content.ReadFromJsonAsync<
                ServiceResult<AgentOrderDetailDto>>();
            if (result?.Success == true && result.Data != null)
            {
                Order = result.Data;
                InitializeDeliveryCredentials();
                return Page();
            }

            ErrorMessage ??= result?.Message ?? "Unable to load this order.";
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

    private void InitializeDeliveryCredentials()
    {
        DeliveryCredentials = Order?.DeliveryFields
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new DeliveryCredentialInput
            {
                FieldKey = x.FieldKey,
                Value = x.Value
            })
            .ToList() ?? new List<DeliveryCredentialInput>();
    }

    private async Task<IActionResult> SignOutExpiredSessionAsync()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["StatusMessage"] = AppMessages.SessionExpired;
        return RedirectToPage("/Account/Login");
    }

    public sealed class DeliveryCredentialInput
    {
        public string FieldKey { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
