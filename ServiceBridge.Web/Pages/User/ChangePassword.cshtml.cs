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
using ServiceBridge.Application.Validation;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Shared.Messages;

namespace ServiceBridge.Web.Pages.User
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ChangePasswordModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public ChangePasswordInputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public class ChangePasswordInputModel
        {
            [Required(ErrorMessage = "Current password is required.")]
            [DataType(DataType.Password)]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "New password is required.")]
            [PasswordPolicy]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Confirm new password is required.")]
            [Compare("NewPassword", ErrorMessage = "New passwords do not match.")]
            [DataType(DataType.Password)]
            public string ConfirmNewPassword { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

            var request = new ChangePasswordRequest
            {
                CurrentPassword = Input.CurrentPassword,
                NewPassword = Input.NewPassword,
                ConfirmNewPassword = Input.ConfirmNewPassword
            };

            HttpResponseMessage response;
            try
            {
                response = await client.PutAsJsonAsync("api/user/password", request);
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
            var result = JsonSerializer.Deserialize<ServiceResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null || !result.Success)
            {
                if (result?.ValidationErrors != null && result.ValidationErrors.Count > 0)
                {
                    foreach (var (field, errors) in result.ValidationErrors)
                    {
                        foreach (var error in errors)
                        {
                            ModelState.AddModelError(string.Empty, error);
                        }
                    }
                    return Page();
                }

                ErrorMessage = result?.Message ?? "Failed to change password.";
                return Page();
            }

            SuccessMessage = "Password changed successfully.";
            ModelState.Clear();
            return Page();
        }
    }
}
