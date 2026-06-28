using System.ComponentModel.DataAnnotations;
using System.Net;
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
public class EditProfileModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public EditProfileModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    public EditProfileInputModel Input { get; set; } = new();

    public string CurrentEmail { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public class EditProfileInputModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
        public string FullName { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var (profile, error) = await LoadProfileAsync();
        if (profile == null)
        {
            ErrorMessage = error;
            return Page();
        }

        Input.FullName = profile.FullName;
        CurrentEmail = profile.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            // Reload current email for display
            var (profile, _) = await LoadProfileAsync();
            CurrentEmail = profile?.Email ?? string.Empty;
            return Page();
        }

        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        var request = new UpdateProfileRequest
        {
            FullName = Input.FullName
        };

        HttpResponseMessage response;
        try
        {
            response = await client.PutAsJsonAsync("api/user/profile", request);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to connect to the server. Please try again later.";
            return Page();
        }

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
            ErrorMessage = result?.Message ?? "Failed to update profile.";
            var (profile, _) = await LoadProfileAsync();
            CurrentEmail = profile?.Email ?? string.Empty;
            return Page();
        }

        // Update cookie claims with new FullName
        await RefreshCookieClaimsAsync(result.Data!);

        SuccessMessage = "Profile updated successfully.";
        CurrentEmail = result.Data!.Email;
        return Page();
    }

    private async Task<(UserProfileResponse? Profile, string? Error)> LoadProfileAsync()
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            var response = await client.GetAsync("api/user/profile");
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["StatusMessage"] = AppMessages.SessionExpired;
                return (null, null); // Will redirect in caller
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ServiceResult<UserProfileResponse>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Success == true && result.Data != null)
            {
                return (result.Data, null);
            }

            return (null, result?.Message ?? "Failed to load profile.");
        }
        catch (HttpRequestException)
        {
            return (null, "Unable to connect to the server.");
        }
    }

    private async Task RefreshCookieClaimsAsync(UserProfileResponse profile)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var currentEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var currentRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, currentUserId ?? profile.UserId.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, currentEmail ?? profile.Email),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, currentRole ?? profile.Role),
            new System.Security.Claims.Claim("FullName", profile.FullName)
        };

        var claimsIdentity = new System.Security.Claims.ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        // Preserve the existing access token
        var existingToken = await HttpContext.GetTokenAsync("access_token");
        var expiresUtc = existingToken != null
            ? (HttpContext.AuthenticateAsync().Result.Properties?.ExpiresUtc)
            : null;

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true
        };

        if (expiresUtc.HasValue)
        {
            authProperties.ExpiresUtc = expiresUtc;
        }

        if (!string.IsNullOrEmpty(existingToken))
        {
            authProperties.StoreTokens(new[]
            {
                new AuthenticationToken
                {
                    Name = "access_token",
                    Value = existingToken
                }
            });
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new System.Security.Claims.ClaimsPrincipal(claimsIdentity),
            authProperties);
    }
}
