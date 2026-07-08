using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class UserProfileClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UserProfileClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<UserProfileApiResult<UserProfileResponse>> GetProfileAsync(
        CancellationToken cancellationToken = default)
    {
        return SendAsync<UserProfileResponse>(
            client => client.GetAsync("api/user/profile", cancellationToken),
            "Unable to load your profile right now.",
            cancellationToken);
    }

    public Task<UserProfileApiResult<UserProfileResponse>> UpdateProfileAsync(
        string fullName,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<UserProfileResponse>(
            client => client.PutAsJsonAsync(
                "api/user/profile",
                new UpdateProfileRequest { FullName = fullName },
                cancellationToken),
            "Unable to update your profile right now.",
            cancellationToken);
    }

    public Task<UserProfileApiResult<UserProfileResponse>> UpdatePhotoAsync(
        string profileImageUrl,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<UserProfileResponse>(
            client => client.PutAsJsonAsync(
                "api/user/photo",
                new UpdateProfilePhotoRequest { ProfileImageUrl = profileImageUrl },
                cancellationToken),
            "Unable to update your profile photo right now.",
            cancellationToken);
    }

    public Task<UserProfileApiResult<bool>> ChangePasswordAsync(
        string currentPassword,
        string newPassword,
        string confirmNewPassword,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PutAsJsonAsync(
                "api/user/password",
                new ChangePasswordRequest
                {
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword,
                    ConfirmNewPassword = confirmNewPassword
                },
                cancellationToken),
            "Unable to change your password right now.",
            cancellationToken);
    }

    private async Task<UserProfileApiResult<T>> SendAsync<T>(
        Func<HttpClient, Task<HttpResponseMessage>> send,
        string fallbackMessage,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            using var response = await send(client);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return UserProfileApiResult<T>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<T>>(cancellationToken);

            if (result?.Success == true && result.Data is not null)
            {
                return UserProfileApiResult<T>.Succeeded(result.Data);
            }

            return UserProfileApiResult<T>.Failed(
                result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                result?.ValidationErrors,
                response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return UserProfileApiResult<T>.Failed(
                "Unable to connect to the profile service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return UserProfileApiResult<T>.Failed(
                "The profile service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return UserProfileApiResult<T>.Failed(
                "The profile service returned an invalid response.");
        }
    }

    private async Task<UserProfileApiResult<bool>> SendCommandAsync(
        Func<HttpClient, Task<HttpResponseMessage>> send,
        string fallbackMessage,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(AppConstants.Api.HttpClientName);

        try
        {
            using var response = await send(client);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return UserProfileApiResult<bool>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult>(cancellationToken);

            return result?.Success == true
                ? UserProfileApiResult<bool>.Succeeded(true)
                : UserProfileApiResult<bool>.Failed(
                    result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                    result?.ValidationErrors,
                    response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return UserProfileApiResult<bool>.Failed(
                "Unable to connect to the profile service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return UserProfileApiResult<bool>.Failed(
                "The profile service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return UserProfileApiResult<bool>.Failed(
                "The profile service returned an invalid response.");
        }
    }

    private static string MessageFor(HttpStatusCode statusCode, string fallbackMessage)
    {
        return statusCode switch
        {
            HttpStatusCode.Forbidden => "You are not allowed to update this profile.",
            HttpStatusCode.NotFound => "The requested profile was not found.",
            HttpStatusCode.Conflict => "The profile changed. Refresh and try again.",
            _ => fallbackMessage
        };
    }
}

public sealed record UserProfileApiResult<T>(
    bool Success,
    bool RequiresAuthentication,
    string? ErrorMessage,
    Dictionary<string, string[]>? ValidationErrors,
    T? Data)
{
    public static UserProfileApiResult<T> Succeeded(T data)
    {
        return new UserProfileApiResult<T>(
            true,
            false,
            null,
            null,
            data);
    }

    public static UserProfileApiResult<T> AuthRequired()
    {
        return new UserProfileApiResult<T>(
            false,
            true,
            "Sign in before managing your profile.",
            null,
            default);
    }

    public static UserProfileApiResult<T> Failed(
        string? errorMessage,
        Dictionary<string, string[]>? validationErrors = null,
        HttpStatusCode statusCode = 0)
    {
        return new UserProfileApiResult<T>(
            false,
            false,
            errorMessage,
            validationErrors,
            default);
    }
}
