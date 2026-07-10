using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class TicketsClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TicketsClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<TicketsApiResult<PagedResultDto<TicketListItemDto>>>
        GetMyTicketsAsync(
            int page,
            int pageSize,
            TicketStatus? status,
            CancellationToken cancellationToken = default)
    {
        var query = $"api/tickets?page={Math.Max(page, 1)}&pageSize={pageSize}";
        if (status.HasValue)
        {
            query += $"&status={Uri.EscapeDataString(status.Value.ToString())}";
        }

        return SendAsync<PagedResultDto<TicketListItemDto>>(
            client => client.GetAsync(query, cancellationToken),
            "Unable to load your support tickets right now.",
            cancellationToken);
    }

    public Task<TicketsApiResult<TicketDetailDto>> GetMyTicketAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TicketDetailDto>(
            client => client.GetAsync($"api/tickets/{ticketId}", cancellationToken),
            "Unable to load this support ticket right now.",
            cancellationToken);
    }

    public Task<TicketsApiResult<TicketDetailDto>> CreateTicketAsync(
        CreateTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TicketDetailDto>(
            client => client.PostAsJsonAsync("api/tickets", request, cancellationToken),
            "Unable to create this support ticket right now.",
            cancellationToken);
    }

    public Task<TicketsApiResult<bool>> AddMyMessageAsync(
        Guid ticketId,
        CreateTicketMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                $"api/tickets/{ticketId}/reply",
                request,
                cancellationToken),
            "Unable to submit this reply right now.",
            cancellationToken);
    }

    public Task<TicketsApiResult<PagedResultDto<TicketListItemDto>>>
        GetAdminTicketsAsync(
            int page,
            int pageSize,
            TicketStatus? status,
            Guid? userId,
            Guid? orderId,
            string? search,
            CancellationToken cancellationToken = default)
    {
        var query = $"api/admin/tickets?page={Math.Max(page, 1)}&pageSize={pageSize}";
        if (status.HasValue)
        {
            query += $"&status={Uri.EscapeDataString(status.Value.ToString())}";
        }

        if (userId.HasValue)
        {
            query += $"&userId={Uri.EscapeDataString(userId.Value.ToString())}";
        }

        if (orderId.HasValue)
        {
            query += $"&orderId={Uri.EscapeDataString(orderId.Value.ToString())}";
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query += $"&search={Uri.EscapeDataString(search.Trim())}";
        }

        return SendAsync<PagedResultDto<TicketListItemDto>>(
            client => client.GetAsync(query, cancellationToken),
            "Unable to load support tickets right now.",
            cancellationToken);
    }

    public Task<TicketsApiResult<TicketDetailDto>> GetAdminTicketAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TicketDetailDto>(
            client => client.GetAsync($"api/admin/tickets/{ticketId}", cancellationToken),
            "Unable to load this support ticket right now.",
            cancellationToken);
    }

    public Task<TicketsApiResult<bool>> AddAdminMessageAsync(
        Guid ticketId,
        CreateTicketMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsJsonAsync(
                $"api/admin/tickets/{ticketId}/reply",
                request,
                cancellationToken),
            "Unable to submit this message right now.",
            cancellationToken);
    }

    public Task<TicketsApiResult<bool>> CloseTicketAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(
            client => client.PostAsync(
                $"api/admin/tickets/{ticketId}/close",
                null,
                cancellationToken),
            "Unable to close this ticket right now.",
            cancellationToken);
    }

    private async Task<TicketsApiResult<T>> SendAsync<T>(
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
                return TicketsApiResult<T>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<T>>(cancellationToken);

            if (result?.Success == true && result.Data is not null)
            {
                return TicketsApiResult<T>.Succeeded(result.Data);
            }

            return TicketsApiResult<T>.Failed(
                result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                result?.ValidationErrors,
                response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return TicketsApiResult<T>.Failed(
                "Unable to connect to the support service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return TicketsApiResult<T>.Failed(
                "The support service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return TicketsApiResult<T>.Failed(
                "The support service returned an invalid response.");
        }
    }

    private async Task<TicketsApiResult<bool>> SendCommandAsync(
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
                return TicketsApiResult<bool>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult>(cancellationToken);

            return result?.Success == true
                ? TicketsApiResult<bool>.Succeeded(true)
                : TicketsApiResult<bool>.Failed(
                    result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                    result?.ValidationErrors,
                    response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return TicketsApiResult<bool>.Failed(
                "Unable to connect to the support service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return TicketsApiResult<bool>.Failed(
                "The support service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return TicketsApiResult<bool>.Failed(
                "The support service returned an invalid response.");
        }
    }

    private static string MessageFor(HttpStatusCode statusCode, string fallbackMessage)
    {
        return statusCode switch
        {
            HttpStatusCode.NotFound => "The requested support ticket was not found.",
            HttpStatusCode.Conflict => "The ticket state changed. Refresh and try again.",
            HttpStatusCode.Forbidden => "You are not allowed to view this support ticket.",
            _ => fallbackMessage
        };
    }
}

public sealed record TicketsApiResult<T>(
    bool Success,
    bool RequiresAuthentication,
    bool NotFound,
    bool Conflict,
    string? ErrorMessage,
    Dictionary<string, string[]>? ValidationErrors,
    T? Data)
{
    public static TicketsApiResult<T> Succeeded(T data)
    {
        return new TicketsApiResult<T>(
            true,
            false,
            false,
            false,
            null,
            null,
            data);
    }

    public static TicketsApiResult<T> AuthRequired()
    {
        return new TicketsApiResult<T>(
            false,
            true,
            false,
            false,
            "Sign in before viewing support tickets.",
            null,
            default);
    }

    public static TicketsApiResult<T> Failed(
        string? errorMessage,
        Dictionary<string, string[]>? validationErrors = null,
        HttpStatusCode statusCode = 0)
    {
        return new TicketsApiResult<T>(
            false,
            false,
            statusCode == HttpStatusCode.NotFound,
            statusCode == HttpStatusCode.Conflict,
            errorMessage,
            validationErrors,
            default);
    }
}
