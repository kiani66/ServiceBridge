using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR.Client;
using ServiceBridge.Application.DTOs;

namespace ServiceBridge.Web.Services;

public sealed class RealtimeClient : IAsyncDisposable
{
    private readonly Func<CancellationToken, Task<string?>> _accessTokenResolver;
    private readonly Func<RealtimeConnectionHandlers, IRealtimeConnection> _connectionFactory;
    private readonly ILogger<RealtimeClient> _logger;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private IRealtimeConnection? _connection;
    private string? _accessToken;
    private bool _disposed;
    private readonly RealtimeSubscriptionStore<RealtimeEventDto> _notificationSubscriptions = new();
    private readonly RealtimeSubscriptionStore<OrderUpdatedRealtimeDto> _orderUpdatedSubscriptions = new();
    private readonly RealtimeSubscriptionStore<OrderAssignedRealtimeDto> _orderAssignedSubscriptions = new();
    private readonly RealtimeSubscriptionStore<CustomerActionChangedRealtimeDto> _customerActionSubscriptions = new();
    private readonly RealtimeSubscriptionStore<TicketUpdatedRealtimeDto> _ticketUpdatedSubscriptions = new();

    public RealtimeClient(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RealtimeClient> logger)
    {
        _logger = logger;
        _accessTokenResolver = cancellationToken =>
            ResolveAccessTokenAsync(httpContextAccessor, cancellationToken);
        _connectionFactory = handlers => BuildConnection(configuration, handlers);
    }

    internal RealtimeClient(
        ILogger<RealtimeClient> logger,
        Func<CancellationToken, Task<string?>> accessTokenResolver,
        Func<RealtimeConnectionHandlers, IRealtimeConnection> connectionFactory)
    {
        _logger = logger;
        _accessTokenResolver = accessTokenResolver;
        _connectionFactory = connectionFactory;
    }

    public IDisposable SubscribeToNotifications(Func<RealtimeEventDto, Task> handler) =>
        _notificationSubscriptions.Subscribe(handler);

    public IDisposable SubscribeToOrderUpdates(Func<OrderUpdatedRealtimeDto, Task> handler) =>
        _orderUpdatedSubscriptions.Subscribe(handler);

    public IDisposable SubscribeToOrderAssignments(Func<OrderAssignedRealtimeDto, Task> handler) =>
        _orderAssignedSubscriptions.Subscribe(handler);

    public IDisposable SubscribeToCustomerActions(Func<CustomerActionChangedRealtimeDto, Task> handler) =>
        _customerActionSubscriptions.Subscribe(handler);

    public IDisposable SubscribeToTicketUpdates(Func<TicketUpdatedRealtimeDto, Task> handler) =>
        _ticketUpdatedSubscriptions.Subscribe(handler);

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (_disposed ||
                (_connection is not null &&
                 _connection.State != HubConnectionState.Disconnected))
            {
                return;
            }

            _accessToken ??= await _accessTokenResolver(cancellationToken);
            if (string.IsNullOrWhiteSpace(_accessToken))
            {
                _logger.LogDebug("Skipping realtime connection because no access token is available.");
                return;
            }

            _connection ??= _connectionFactory(CreateHandlers());

            await _connection.StartAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to start realtime connection.");
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (_disposed ||
                _connection is null ||
                _connection.State == HubConnectionState.Disconnected)
            {
                return;
            }

            await _connection.StopAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to stop realtime connection cleanly.");
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        IRealtimeConnection? connection;

        await _lifecycleGate.WaitAsync();
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            connection = _connection;
            _connection = null;
            _accessToken = null;
        }
        finally
        {
            _lifecycleGate.Release();
        }

        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }

    private RealtimeConnectionHandlers CreateHandlers()
    {
        return new RealtimeConnectionHandlers(
            message => DispatchAsync(_notificationSubscriptions, message),
            message => DispatchAsync(_orderUpdatedSubscriptions, message),
            message => DispatchAsync(_orderAssignedSubscriptions, message),
            message => DispatchAsync(_customerActionSubscriptions, message),
            message => DispatchAsync(_ticketUpdatedSubscriptions, message));
    }

    private IRealtimeConnection BuildConnection(
        IConfiguration configuration,
        RealtimeConnectionHandlers handlers)
    {
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];
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
            handlers.NotificationReceived);
        connection.On<OrderUpdatedRealtimeDto>(
            "orderUpdated",
            handlers.OrderUpdated);
        connection.On<OrderAssignedRealtimeDto>(
            "orderAssigned",
            handlers.OrderAssigned);
        connection.On<CustomerActionChangedRealtimeDto>(
            "customerActionChanged",
            handlers.CustomerActionChanged);
        connection.On<TicketUpdatedRealtimeDto>(
            "ticketUpdated",
            handlers.TicketUpdated);

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

        return new SignalRRealtimeConnection(connection);
    }

    private async Task DispatchAsync<T>(RealtimeSubscriptionStore<T> subscriptions, T message)
    {
        var registrations = subscriptions.Snapshot();
        foreach (var registration in registrations)
        {
            await InvokeHandlerSafelyAsync(registration, message);
        }
    }

    private async Task InvokeHandlerSafelyAsync<T>(
        RealtimeSubscription<T> registration,
        T message)
    {
        if (!registration.IsActive)
        {
            return;
        }

        try
        {
            await registration.Handler(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "A realtime UI refresh handler failed.");
        }
    }

    private static async Task<string?> ResolveAccessTokenAsync(
        IHttpContextAccessor httpContextAccessor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var httpContext = httpContextAccessor.HttpContext;
        return httpContext is null
            ? null
            : await httpContext.GetTokenAsync("access_token");
    }
}

internal sealed class RealtimeSubscriptionStore<T>
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, RealtimeSubscription<T>> _registrations = new();

    public IDisposable Subscribe(Func<T, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var registration = new RealtimeSubscription<T>(Guid.NewGuid(), handler, Remove);
        lock (_sync)
        {
            _registrations.Add(registration.Id, registration);
        }

        return registration;
    }

    public IReadOnlyList<RealtimeSubscription<T>> Snapshot()
    {
        lock (_sync)
        {
            return _registrations.Values.ToArray();
        }
    }

    private void Remove(Guid id)
    {
        lock (_sync)
        {
            _registrations.Remove(id);
        }
    }
}

internal sealed class RealtimeSubscription<T> : IDisposable
{
    private readonly Action<Guid> _remove;
    private int _active = 1;

    public RealtimeSubscription(Guid id, Func<T, Task> handler, Action<Guid> remove)
    {
        Id = id;
        Handler = handler;
        _remove = remove;
    }

    public Guid Id { get; }
    public Func<T, Task> Handler { get; }
    public bool IsActive => Volatile.Read(ref _active) == 1;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _active, 0) == 0)
        {
            return;
        }

        _remove(Id);
    }
}

public sealed class RealtimeSubscriptionCollection : IDisposable
{
    private readonly object _sync = new();
    private readonly List<IDisposable> _subscriptions = new();
    private bool _disposed;

    public void Add(IDisposable subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        lock (_sync)
        {
            if (_disposed)
            {
                subscription.Dispose();
                return;
            }

            _subscriptions.Add(subscription);
        }
    }

    public void Dispose()
    {
        IDisposable[] subscriptions;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            subscriptions = _subscriptions.ToArray();
            _subscriptions.Clear();
        }

        foreach (var subscription in subscriptions)
        {
            subscription.Dispose();
        }
    }
}

internal sealed record RealtimeConnectionHandlers(
    Func<RealtimeEventDto, Task> NotificationReceived,
    Func<OrderUpdatedRealtimeDto, Task> OrderUpdated,
    Func<OrderAssignedRealtimeDto, Task> OrderAssigned,
    Func<CustomerActionChangedRealtimeDto, Task> CustomerActionChanged,
    Func<TicketUpdatedRealtimeDto, Task> TicketUpdated);

internal interface IRealtimeConnection : IAsyncDisposable
{
    HubConnectionState State { get; }
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

internal sealed class SignalRRealtimeConnection(HubConnection connection) : IRealtimeConnection
{
    public HubConnectionState State => connection.State;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return connection.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return connection.StopAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return connection.DisposeAsync();
    }
}
