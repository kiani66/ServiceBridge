using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Shared.Constants;

namespace ServiceBridge.Infrastructure.Services;

public class HttpSmsProvider : ISmsProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly SmsOptions _options;
    private readonly ILogger<HttpSmsProvider> _logger;

    public HttpSmsProvider(
        HttpClient httpClient,
        IOptions<SmsOptions> options,
        ILogger<HttpSmsProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ServiceResult> SendAsync(
        MessageOutbox message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (message.Channel != MessageChannel.Sms)
        {
            return DeliveryFailure("HTTP SMS provider only supports SMS messages.");
        }

        if (!_options.Enabled)
        {
            _logger.LogWarning(
                "SMS delivery is disabled for outbox entry {MessageId}.",
                message.Id);

            return DeliveryFailure("SMS delivery is disabled.");
        }

        if (!IsHttpProvider(_options.Provider))
        {
            return DeliveryFailure("Configured SMS provider is not supported by the HTTP SMS adapter.");
        }

        using var timeoutCts = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            using var request = CreateRequest(message);
            using var response = await _httpClient.SendAsync(request, timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "HTTP SMS delivery failed for outbox entry {MessageId} with status {StatusCode}.",
                    message.Id,
                    (int)response.StatusCode);

                return DeliveryFailure(
                    $"HTTP SMS delivery failed with status {(int)response.StatusCode}.");
            }

            _logger.LogInformation(
                "HTTP SMS sent for outbox entry {MessageId}.",
                message.Id);

            return ServiceResult.Ok("SMS delivered by HTTP provider.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogWarning(
                exception,
                "HTTP SMS delivery timed out for outbox entry {MessageId}.",
                message.Id);

            return DeliveryFailure("HTTP SMS delivery timed out.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                exception,
                "HTTP SMS delivery failed for outbox entry {MessageId}.",
                message.Id);

            return DeliveryFailure("HTTP SMS delivery failed.");
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or UriFormatException or JsonException)
        {
            _logger.LogWarning(
                exception,
                "HTTP SMS request could not be created for outbox entry {MessageId}.",
                message.Id);

            return DeliveryFailure("HTTP SMS request is invalid.");
        }
    }

    private HttpRequestMessage CreateRequest(MessageOutbox message)
    {
        var payload = new HttpSmsRequest(
            message.Recipient.Trim(),
            _options.Sender.Trim(),
            message.Body);
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl.Trim())
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _options.ApiKey.Trim());

        return request;
    }

    private static bool IsHttpProvider(string provider)
    {
        return string.Equals(provider.Trim(), "Http", StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceResult DeliveryFailure(string message)
    {
        return ServiceResult.Fail(
            message,
            AppConstants.ErrorCodes.Conflict,
            409);
    }

    private sealed record HttpSmsRequest(
        string Recipient,
        string Sender,
        string Message);
}
