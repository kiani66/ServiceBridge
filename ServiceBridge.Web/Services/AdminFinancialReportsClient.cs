using System.Net;
using System.Net.Http.Json;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Web.Services;

public sealed class AdminFinancialReportsClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AdminFinancialReportsClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<AdminOrdersApiResult<AdminFinancialReportDto>> GetFinancialReportAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        PaymentStatus? paymentStatus,
        RefundStatus? refundStatus,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<AdminFinancialReportDto>(
            client => client.GetAsync(
                $"api/admin/reports/financial?{BuildQuery(fromUtc, toUtc, paymentStatus, refundStatus)}",
                cancellationToken),
            "Unable to load financial reports right now.",
            cancellationToken);
    }

    public Task<AdminOrdersApiResult<AdminFinancialCsvExportDto>>
        ExportFinancialReportCsvAsync(
            DateTime? fromUtc,
            DateTime? toUtc,
            PaymentStatus? paymentStatus,
            RefundStatus? refundStatus,
            CancellationToken cancellationToken = default)
    {
        return SendAsync<AdminFinancialCsvExportDto>(
            client => client.GetAsync(
                $"api/admin/reports/financial.csv?{BuildQuery(fromUtc, toUtc, paymentStatus, refundStatus)}",
                cancellationToken),
            "Unable to export financial reports right now.",
            cancellationToken);
    }

    private async Task<AdminOrdersApiResult<T>> SendAsync<T>(
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
                return AdminOrdersApiResult<T>.AuthRequired();
            }

            if (typeof(T) == typeof(AdminFinancialCsvExportDto) &&
                response.IsSuccessStatusCode)
            {
                var csv = await response.Content.ReadAsStringAsync(cancellationToken);
                var fileName = response.Content.Headers.ContentDisposition?.FileNameStar ??
                    response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ??
                    "admin-financial-report.csv";
                return AdminOrdersApiResult<T>.Succeeded((T)(object)new AdminFinancialCsvExportDto
                {
                    Content = csv,
                    FileName = fileName
                });
            }

            var result = await response.Content
                .ReadFromJsonAsync<ServiceResult<T>>(cancellationToken);
            if (result?.Success == true && result.Data is not null)
            {
                return AdminOrdersApiResult<T>.Succeeded(result.Data);
            }

            return AdminOrdersApiResult<T>.Failed(
                result?.Message ?? MessageFor(response.StatusCode, fallbackMessage),
                result?.ValidationErrors,
                response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return AdminOrdersApiResult<T>.Failed(
                "Unable to connect to the financial reports service. Please try again later.");
        }
        catch (NotSupportedException)
        {
            return AdminOrdersApiResult<T>.Failed(
                "The financial reports service returned an unsupported response.");
        }
        catch (System.Text.Json.JsonException)
        {
            return AdminOrdersApiResult<T>.Failed(
                "The financial reports service returned an invalid response.");
        }
    }

    private static string BuildQuery(
        DateTime? fromUtc,
        DateTime? toUtc,
        PaymentStatus? paymentStatus,
        RefundStatus? refundStatus)
    {
        var parameters = new List<string>();
        if (fromUtc.HasValue)
        {
            parameters.Add($"fromUtc={Uri.EscapeDataString(fromUtc.Value.ToUniversalTime().ToString("O"))}");
        }

        if (toUtc.HasValue)
        {
            parameters.Add($"toUtc={Uri.EscapeDataString(toUtc.Value.ToUniversalTime().ToString("O"))}");
        }

        if (paymentStatus.HasValue)
        {
            parameters.Add($"paymentStatus={Uri.EscapeDataString(paymentStatus.Value.ToString())}");
        }

        if (refundStatus.HasValue)
        {
            parameters.Add($"refundStatus={Uri.EscapeDataString(refundStatus.Value.ToString())}");
        }

        return string.Join("&", parameters);
    }

    private static string MessageFor(HttpStatusCode statusCode, string fallbackMessage)
    {
        return statusCode switch
        {
            HttpStatusCode.Forbidden => "You are not allowed to view financial reports.",
            _ => fallbackMessage
        };
    }
}
