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

namespace ServiceBridge.Web.Pages.Orders;

[Authorize(Roles = AppConstants.Roles.User)]
public class DetailsModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DetailsModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public UserOrderDetailDto? Order { get; private set; }
    public string? ErrorMessage { get; private set; }

    [BindProperty]
    public Guid CustomerActionRequestId { get; set; }

    [BindProperty]
    public string CustomerActionResponse { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!Request.HasFormContentType)
        {
            return LocalRedirect($"/ui/orders/{id}");
        }

        return await LoadOrderAsync(id);
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id)
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var response = await client.PostAsync($"api/orders/my/{id}/cancel", null);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var result = await response.Content.ReadFromJsonAsync<ServiceResult>();
            if (result?.Success == true)
            {
                TempData["StatusMessage"] = result.Message ?? "Order cancelled successfully.";
                return RedirectToPage(new { id });
            }

            ErrorMessage = result?.Message ?? "Unable to cancel this order.";
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

        return await LoadOrderAsync(id);
    }

    public async Task<IActionResult> OnPostInitiatePaymentAsync(Guid id)
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var initiateResponse = await client.PostAsJsonAsync(
                "api/payments/initiate",
                new InitiatePaymentRequest
                {
                    OrderId = id,
                    PaymentId = Guid.NewGuid()
                });
            if (initiateResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var initiateResult = await initiateResponse.Content
                .ReadFromJsonAsync<ServiceResult<PaymentInitiatedResponse>>();
            if (initiateResult?.Success != true || initiateResult.Data == null)
            {
                ErrorMessage = initiateResult?.Message ?? "Unable to start payment for this order.";
                return await LoadOrderAsync(id);
            }

            var mockResponse = await client.PostAsJsonAsync(
                "api/payments/mock",
                new MockPaymentRequest
                {
                    PaymentId = initiateResult.Data.PaymentId,
                    Succeed = true
                });
            if (mockResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var mockResult = await mockResponse.Content
                .ReadFromJsonAsync<ServiceResult<MockPaymentResultResponse>>();
            if (mockResult?.Success == true && mockResult.Data != null)
            {
                var route = mockResult.Data.Status == Domain.Enums.PaymentStatus.Completed
                    ? "success"
                    : "failure";

                return Redirect(
                    $"/ui/payments/{route}/{mockResult.Data.OrderId}?paymentId={mockResult.Data.PaymentId}");
            }

            ErrorMessage = mockResult?.Message ?? "Unable to process the mock payment.";
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

        return await LoadOrderAsync(id);
    }

    public async Task<IActionResult> OnPostRespondToCustomerActionAsync(Guid id)
    {
        if (CustomerActionRequestId == Guid.Empty ||
            string.IsNullOrWhiteSpace(CustomerActionResponse))
        {
            ErrorMessage = "Enter a response to the customer action request.";
            return await LoadOrderAsync(id);
        }

        return await PostOrderActionAsync(
            id,
            $"api/orders/my/{id}/customer-actions/respond",
            new RespondToCustomerActionRequest
            {
                RequestId = CustomerActionRequestId,
                Response = CustomerActionResponse
            },
            "Response submitted successfully.");
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

    private async Task<IActionResult> PostOrderActionAsync<TRequest>(
        Guid id,
        string endpoint,
        TRequest request,
        string successMessage)
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
                TempData["StatusMessage"] = result.Message ?? successMessage;
                return RedirectToPage(new { id });
            }

            ErrorMessage = result?.Message ?? "Unable to update this order.";
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

        return await LoadOrderAsync(id);
    }

    private async Task<IActionResult> LoadOrderAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return RedirectToPage("/Orders/Index");
        }

        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var response = await client.GetAsync($"api/orders/my/{id}");
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<UserOrderDetailDto>>();

            if (result?.Success == true && result.Data != null)
            {
                Order = result.Data;
                return Page();
            }

            ErrorMessage ??= result?.Message ?? "Unable to load this order.";
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

        return Page();
    }

    private async Task<IActionResult> SignOutExpiredSessionAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["StatusMessage"] = AppMessages.SessionExpired;
        return RedirectToPage("/Account/Login");
    }
}
