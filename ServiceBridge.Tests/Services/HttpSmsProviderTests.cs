using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class HttpSmsProviderTests
{
    [Fact]
    public async Task SendAsync_WhenSmsDisabled_ReturnsFailureWithoutHttpCall()
    {
        var handler = new RecordingHttpMessageHandler(HttpStatusCode.OK);
        var provider = CreateProvider(new SmsOptions { Enabled = false }, handler);
        var message = CreateMessage();

        var result = await provider.SendAsync(message);

        Assert.False(result.Success);
        Assert.Equal("SMS delivery is disabled.", result.Message);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task SendAsync_WhenEnabled_SendsExpectedHttpRequest()
    {
        var handler = new RecordingHttpMessageHandler(HttpStatusCode.Accepted);
        var options = CreateEnabledOptions();
        var provider = CreateProvider(options, handler);
        var message = CreateMessage();

        var result = await provider.SendAsync(message);

        Assert.True(result.Success);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal(HttpMethod.Post, handler.Request?.Method);
        Assert.Equal(options.BaseUrl, handler.Request?.RequestUri?.ToString());
        Assert.Equal("Bearer", handler.Request?.Headers.Authorization?.Scheme);
        Assert.Equal(options.ApiKey, handler.Request?.Headers.Authorization?.Parameter);
        using var payload = JsonDocument.Parse(handler.Body);
        Assert.Equal("+989120000000", payload.RootElement.GetProperty("recipient").GetString());
        Assert.Equal("ServiceBridge", payload.RootElement.GetProperty("sender").GetString());
        Assert.Equal("Verification code: 123456", payload.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task SendAsync_WhenHttpResponseIsNotSuccessful_ReturnsFailure()
    {
        var handler = new RecordingHttpMessageHandler(HttpStatusCode.BadGateway);
        var provider = CreateProvider(CreateEnabledOptions(), handler);
        var message = CreateMessage();

        var result = await provider.SendAsync(message);

        Assert.False(result.Success);
        Assert.Equal("HTTP SMS delivery failed with status 502.", result.Message);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task SendAsync_WhenCancellationRequested_ThrowsWithoutHttpCall()
    {
        var handler = new RecordingHttpMessageHandler(HttpStatusCode.OK);
        var provider = CreateProvider(CreateEnabledOptions(), handler);
        var message = CreateMessage();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => provider.SendAsync(message, cancellationTokenSource.Token));
        Assert.Equal(0, handler.CallCount);
    }

    private static HttpSmsProvider CreateProvider(
        SmsOptions options,
        RecordingHttpMessageHandler handler)
    {
        return new HttpSmsProvider(
            new HttpClient(handler),
            Options.Create(options),
            NullLogger<HttpSmsProvider>.Instance);
    }

    private static SmsOptions CreateEnabledOptions()
    {
        return new SmsOptions
        {
            Enabled = true,
            Provider = "Http",
            BaseUrl = "https://sms.example.local/send",
            ApiKey = "test-api-key",
            Sender = "ServiceBridge",
            TimeoutSeconds = 30
        };
    }

    private static MessageOutbox CreateMessage()
    {
        return MessageOutbox.Create(
            MessageChannel.Sms,
            "+989120000000",
            null,
            "Verification code: 123456",
            null,
            null,
            null,
            DateTime.UtcNow);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public RecordingHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        public int CallCount { get; private set; }
        public HttpRequestMessage? Request { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Request = request;
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent("{}")
            };
        }
    }
}
