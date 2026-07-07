using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using ServiceBridge.Application.DTOs;

namespace ServiceBridge.Web.Services;

public sealed class OrderRealtimeClient : IAsyncDisposable
{
    private const string ClientMethodName = "orderUpdated";

    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<OrderRealtimeClient> _logger;
    private HubConnection? _connection;
    private string? _accessToken;

    public OrderRealtimeClient(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<OrderRealtimeClient> logger)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public event Func<OrderUpdatedRealtimeMessage, Task>? OrderUpdated;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is not null &&
            _connection.State != HubConnectionState.Disconnected)
        {
            return;
        }

        _accessToken ??= await ResolveAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            _logger.LogDebug("Skipping order realtime connection because no access token is available.");
            return;
        }

        _connection ??= BuildConnection();

        try
        {
            await _connection.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to start order realtime connection.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    private HubConnection BuildConnection()
    {
        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            throw new InvalidOperationException(
                "Configuration value 'ApiSettings:BaseUrl' must be configured.");
        }

        var hubUrl = new Uri(new Uri(apiBaseUrl), "hubs/order-updates");
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider =
                    () => Task.FromResult(_accessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<OrderUpdatedRealtimeMessage>(
            ClientMethodName,
            async message =>
            {
                var handler = OrderUpdated;
                if (handler is not null)
                {
                    await handler(message);
                }
            });

        return connection;
    }

    private async Task<string?> ResolveAccessTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext is null
            ? null
            : await httpContext.GetTokenAsync("access_token");
    }
}
