using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class NotificationsClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public NotificationsClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<NotificationsApiResult<PagedResultDto<NotificationDto>>>
        GetNotificationsAsync(
            int page,
            int pageSize,
            bool unreadOnly = false,
            CancellationToken cancellationToken = default)
    {
        var query =
            $"api/notifications?page={Math.Max(page, 1)}&pageSize={pageSize}&unreadOnly={unreadOnly}";
        return SendAsync<PagedResultDto<NotificationDto>>(
            client => client.GetAsync(query, cancellationToken),
            "Unable to load notifications right now.",
            cancellationToken);
    }

    public Task<NotificationsApiResult<UnreadNotificationCountDto>>
        GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync<UnreadNotificationCountDto>(
            client => client.GetAsync(
                "api/notifications/unread-count",
                cancellationToken),
            "Unable to load notification count right now.",
            cancellationToken);
    }

    public Task<NotificationsApiResult<bool>> MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsync(
                $"api/notifications/{notificationId}/read",
                content: null,
                cancellationToken),
            "Unable to mark this notification as read.",
            cancellationToken);
    }

    public Task<NotificationsApiResult<bool>> MarkAllAsReadAsync(
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsync(
                "api/notifications/read-all",
                content: null,
                cancellationToken),
            "Unable to mark notifications as read.",
            cancellationToken);
    }

    private async Task<NotificationsApiResult<T>> SendAsync<T>(
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
                return NotificationsApiResult<T>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<T>>(cancellationToken);

            if (result?.Success == true && result.Data is not null)
            {
                return NotificationsApiResult<T>.Succeeded(result.Data);
            }

            return NotificationsApiResult<T>.Failed(
                result?.Message ?? fallbackMessage,
                result?.ValidationErrors,
                response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return NotificationsApiResult<T>.Failed(
                "Unable to connect to the notification service.");
        }
        catch (NotSupportedException)
        {
            return NotificationsApiResult<T>.Failed(
                "The notification service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return NotificationsApiResult<T>.Failed(
                "The notification service returned an invalid response.");
        }
    }

    private async Task<NotificationsApiResult<bool>> SendCommandAsync(
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
                return NotificationsApiResult<bool>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult>(cancellationToken);

            return result?.Success == true
                ? NotificationsApiResult<bool>.Succeeded(true)
                : NotificationsApiResult<bool>.Failed(
                    result?.Message ?? fallbackMessage,
                    result?.ValidationErrors,
                    response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return NotificationsApiResult<bool>.Failed(
                "Unable to connect to the notification service.");
        }
        catch (NotSupportedException)
        {
            return NotificationsApiResult<bool>.Failed(
                "The notification service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return NotificationsApiResult<bool>.Failed(
                "The notification service returned an invalid response.");
        }
    }
}

public sealed record NotificationsApiResult<T>(
    bool Success,
    bool RequiresAuthentication,
    string? ErrorMessage,
    Dictionary<string, string[]>? ValidationErrors,
    T? Data)
{
    public static NotificationsApiResult<T> Succeeded(T data)
    {
        return new NotificationsApiResult<T>(
            true,
            false,
            null,
            null,
            data);
    }

    public static NotificationsApiResult<T> AuthRequired()
    {
        return new NotificationsApiResult<T>(
            false,
            true,
            "Sign in before viewing notifications.",
            null,
            default);
    }

    public static NotificationsApiResult<T> Failed(
        string? errorMessage,
        Dictionary<string, string[]>? validationErrors = null,
        HttpStatusCode statusCode = 0)
    {
        return new NotificationsApiResult<T>(
            false,
            false,
            errorMessage,
            validationErrors,
            default);
    }
}
