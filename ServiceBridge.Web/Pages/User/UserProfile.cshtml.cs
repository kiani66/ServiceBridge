using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Web.Pages.User;

[Authorize]
public class UserProfileModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UserProfileModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public UserProfileResponse? Profile { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!Request.HasFormContentType)
        {
            return LocalRedirect("/ui/profile");
        }

        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync("api/user/profile");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to connect to the server. Please try again later.";
            return Page();
        }

        // If API returns 401, the JWT has expired — sign out and redirect to Login
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["StatusMessage"] = AppMessages.SessionExpired;
            return RedirectToPage("/Account/Login");
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ServiceResult<UserProfileResponse>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result == null || !result.Success)
        {
            ErrorMessage = result?.Message ?? "Failed to load profile.";
            return Page();
        }

        Profile = result.Data;
        return Page();
    }
}
