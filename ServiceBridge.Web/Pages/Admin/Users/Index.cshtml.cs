using System.ComponentModel.DataAnnotations;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Application.Validation;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Web.Pages.Admin.Users;

[Authorize(Roles = AppConstants.Roles.Admin)]
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public PagedResultDto<AdminUserListItemDto> Users { get; private set; } =
        new();
    public string? ErrorMessage { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty]
    public ResetPasswordInput Input { get; set; } = new();

    public Task<IActionResult> OnGetAsync()
    {
        return LoadAsync();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync()
    {
        if (!ModelState.IsValid || Input.UserId == Guid.Empty)
        {
            ErrorMessage = "Enter a valid new password.";
            return await LoadAsync();
        }

        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var response = await client.PostAsJsonAsync(
                $"api/admin/users/{Input.UserId}/reset-password",
                new AdminResetPasswordRequest
                {
                    NewPassword = Input.NewPassword
                });

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<bool>>();
            if (result?.Success == true)
            {
                TempData["StatusMessage"] =
                    result.Message ?? "Password reset successfully.";
                return RedirectToPage(new
                {
                    pageNumber = PageNumber,
                    search = Search
                });
            }

            ErrorMessage = result?.Message ?? "Unable to reset the password.";
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

    public async Task<IActionResult> OnPostCreateAgentAsync(
        CreateAgentRequest agent)
    {
        ModelState.Clear();
        if (!TryValidateModel(agent, nameof(agent)))
        {
            ErrorMessage = "Review the agent fields and try again.";
            return await LoadAsync();
        }

        return await SendAsync(
            HttpMethod.Post,
            "api/admin/users/agents",
            agent,
            "Agent created successfully.");
    }

    public Task<IActionResult> OnPostSetStatusAsync(
        Guid userId,
        bool isActive)
    {
        return SendAsync(
            HttpMethod.Patch,
            $"api/admin/users/{userId}/status",
            new SetUserStatusRequest { IsActive = isActive },
            "User status updated successfully.");
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
                return RedirectToPage(new
                {
                    pageNumber = PageNumber,
                    search = Search
                });
            }

            ErrorMessage = result?.Message ?? "Unable to update the user.";
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
            var query = $"api/admin/users?page={PageNumber}&pageSize=20";
            if (!string.IsNullOrWhiteSpace(Search))
            {
                query += $"&search={Uri.EscapeDataString(Search.Trim())}";
            }

            var response = await client.GetAsync(query);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return await SignOutExpiredSessionAsync();
            }

            var result = await response.Content.ReadFromJsonAsync<
                ServiceResult<PagedResultDto<AdminUserListItemDto>>>();
            if (result?.Success == true && result.Data != null)
            {
                Users = result.Data;
                return Page();
            }

            ErrorMessage ??= result?.Message ?? "Unable to load users.";
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

    public class ResetPasswordInput
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [PasswordPolicy]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
