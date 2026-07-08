using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using ServiceBridge.Application.DTOs;

namespace ServiceBridge.Web.Services;

public sealed class RealtimeClient : IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RealtimeClient> _logger;
    private HubConnection? _connection;
    private string? _accessToken;

    public RealtimeClient(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RealtimeClient> logger)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public event Func<RealtimeEventDto, Task>? NotificationReceived;
    public event Func<OrderUpdatedRealtimeDto, Task>? OrderUpdated;
    public event Func<OrderAssignedRealtimeDto, Task>? OrderAssigned;
    public event Func<CustomerActionChangedRealtimeDto, Task>? CustomerActionChanged;
    public event Func<TicketUpdatedRealtimeDto, Task>? TicketUpdated;

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
            _logger.LogDebug("Skipping realtime connection because no access token is available.");
            return;
        }

        _connection ??= BuildConnection();

        try
        {
            await _connection.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to start realtime connection.");
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

        var hubUrl = new Uri(new Uri(apiBaseUrl), "realtimeHub");
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider =
                    () => Task.FromResult(_accessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<RealtimeEventDto>(
            "notificationReceived",
            message => DispatchAsync(NotificationReceived, message));
        connection.On<OrderUpdatedRealtimeDto>(
            "orderUpdated",
            message => DispatchAsync(OrderUpdated, message));
        connection.On<OrderAssignedRealtimeDto>(
            "orderAssigned",
            message => DispatchAsync(OrderAssigned, message));
        connection.On<CustomerActionChangedRealtimeDto>(
            "customerActionChanged",
            message => DispatchAsync(CustomerActionChanged, message));
        connection.On<TicketUpdatedRealtimeDto>(
            "ticketUpdated",
            message => DispatchAsync(TicketUpdated, message));

        connection.Closed += exception =>
        {
            if (exception is not null)
            {
                _logger.LogWarning(exception, "Realtime connection closed.");
            }

            return Task.CompletedTask;
        };

        connection.Reconnecting += exception =>
        {
            if (exception is not null)
            {
                _logger.LogDebug(exception, "Realtime connection reconnecting.");
            }

            return Task.CompletedTask;
        };

        connection.Reconnected += _ =>
        {
            _logger.LogDebug("Realtime connection reconnected.");
            return Task.CompletedTask;
        };

        return connection;
    }

    private async Task DispatchAsync<T>(
        Func<T, Task>? handler,
        T message)
    {
        if (handler is null)
        {
            return;
        }

        await handler(message);
    }

    private async Task<string?> ResolveAccessTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext is null
            ? null
            : await httpContext.GetTokenAsync("access_token");
    }
}
