using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Web.Navigation;

namespace ServiceBridge.Web.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public LoginModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [BindProperty]
    public LoginInputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class LoginInputModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
        public string Password { get; set; } = string.Empty;
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

        var loginRequest = new UserLoginRequest
        {
            Email = Input.Email,
            Password = Input.Password
        };

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("api/auth/user/login", loginRequest);
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to connect to the server. Please try again later.";
            return Page();
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ServiceResult<LoginResponseDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result == null || !result.Success)
        {
            ErrorMessage = result?.Message ?? "Login failed. Please try again.";
            return Page();
        }

        var loginData = result.Data!;
        var landingPage = RoleNavigation.GetLandingPage(loginData.Role);
        if (landingPage == null)
        {
            return Forbid();
        }

        // Create claims for cookie authentication
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, loginData.UserId.ToString()),
            new Claim(ClaimTypes.Name, loginData.Email),
            new Claim(ClaimTypes.Role, loginData.Role),
            new Claim("FullName", loginData.FullName)
        };

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        // Store JWT access token inside AuthenticationProperties
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = loginData.ExpiresAtUtc
        };

        authProperties.StoreTokens(new[]
        {
            new AuthenticationToken
            {
                Name = "access_token",
                Value = loginData.AccessToken
            }
        });

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return LocalRedirect(landingPage);
    }
}
