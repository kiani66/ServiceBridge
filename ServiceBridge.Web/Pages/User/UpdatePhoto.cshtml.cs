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
public class UpdatePhotoModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UpdatePhotoModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    public UpdatePhotoInputModel Input { get; set; } = new();

    public string? CurrentPhotoUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public class UpdatePhotoInputModel
    {
        [Required(ErrorMessage = "Profile image URL is required.")]
        [MaxLength(500, ErrorMessage = "URL cannot exceed 500 characters.")]
        [Url(ErrorMessage = "Please enter a valid URL.")]
        public string ProfileImageUrl { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var (profile, error) = await LoadProfileAsync();
        if (profile == null)
        {
            if (error == null) return RedirectToPage("/Account/Login");
            ErrorMessage = error;
            return Page();
        }

        CurrentPhotoUrl = profile.ProfileImageUrl;
        Input.ProfileImageUrl = profile.ProfileImageUrl ?? string.Empty;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            var (profile, _) = await LoadProfileAsync();
            CurrentPhotoUrl = profile?.ProfileImageUrl;
            return Page();
        }

        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        var request = new UpdateProfilePhotoRequest
        {
            ProfileImageUrl = Input.ProfileImageUrl
        };

        HttpResponseMessage response;
        try
        {
            response = await client.PutAsJsonAsync("api/user/photo", request);
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
            ErrorMessage = result?.Message ?? "Failed to update photo.";
            return Page();
        }

        CurrentPhotoUrl = result.Data?.ProfileImageUrl;
        SuccessMessage = "Profile photo updated successfully.";
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
                return (null, null);
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
}
