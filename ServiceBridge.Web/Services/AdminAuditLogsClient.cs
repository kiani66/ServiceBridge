using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class AdminAuditLogsClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AdminAuditLogsClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<AdminAuditLogsApiResult<PagedResultDto<AuditLogListItemDto>>>
        GetAuditLogsAsync(
            int page,
            int pageSize,
            Guid? actorId,
            string? entityName,
            Guid? entityId,
            AuditActionType? actionType,
            CancellationToken cancellationToken = default)
    {
        var query = $"api/admin/audit-logs?page={Math.Max(page, 1)}&pageSize={pageSize}";

        if (actorId.HasValue)
        {
            query += $"&actorId={actorId.Value}";
        }

        AppendQuery(ref query, "entityName", entityName);

        if (entityId.HasValue)
        {
            query += $"&entityId={entityId.Value}";
        }

        if (actionType.HasValue)
        {
            query += $"&actionType={Uri.EscapeDataString(actionType.Value.ToString())}";
        }

        return SendAsync<PagedResultDto<AuditLogListItemDto>>(
            client => client.GetAsync(query, cancellationToken),
            "Unable to load audit logs right now.",
            cancellationToken);
    }

    private async Task<AdminAuditLogsApiResult<T>> SendAsync<T>(
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
                return AdminAuditLogsApiResult<T>.AuthRequired();
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<T>>(cancellationToken);

            if (result?.Success == true && result.Data is not null)
            {
                return AdminAuditLogsApiResult<T>.Succeeded(result.Data);
            }

            return AdminAuditLogsApiResult<T>.Failed(
                result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                result?.ValidationErrors,
                response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return AdminAuditLogsApiResult<T>.Failed(
                "Unable to connect to the audit log service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return AdminAuditLogsApiResult<T>.Failed(
                "The audit log service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return AdminAuditLogsApiResult<T>.Failed(
                "The audit log service returned an invalid response.");
        }
    }

    private static string MessageFor(HttpStatusCode statusCode, string fallbackMessage)
    {
        return statusCode switch
        {
            HttpStatusCode.Forbidden => "You are not allowed to view audit logs.",
            _ => fallbackMessage
        };
    }

    private static void AppendQuery(ref string query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query += $"&{name}={Uri.EscapeDataString(value.Trim())}";
        }
    }
}

public sealed record AdminAuditLogsApiResult<T>(
    bool Success,
    bool RequiresAuthentication,
    string? ErrorMessage,
    Dictionary<string, string[]>? ValidationErrors,
    T? Data)
{
    public static AdminAuditLogsApiResult<T> Succeeded(T data)
    {
        return new AdminAuditLogsApiResult<T>(
            true,
            false,
            null,
            null,
            data);
    }

    public static AdminAuditLogsApiResult<T> AuthRequired()
    {
        return new AdminAuditLogsApiResult<T>(
            false,
            true,
            "Sign in as an admin before viewing audit logs.",
            null,
            default);
    }

    public static AdminAuditLogsApiResult<T> Failed(
        string? errorMessage,
        Dictionary<string, string[]>? validationErrors = null,
        HttpStatusCode statusCode = 0)
    {
        return new AdminAuditLogsApiResult<T>(
            false,
            false,
            errorMessage,
            validationErrors,
            default);
    }
}
