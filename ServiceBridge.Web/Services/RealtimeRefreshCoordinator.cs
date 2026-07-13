namespace ServiceBridge.Web.Services;

public sealed class RealtimeRefreshCoordinator : IDisposable
{
    private readonly object _sync = new();
    private readonly Func<Task> _refresh;
    private readonly CoalescingAsyncAction _coalescingAction;
    private bool _disposed;

    public RealtimeRefreshCoordinator(Func<Task> refresh)
    {
        _refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));
        _coalescingAction = new CoalescingAsyncAction(RunIfAliveAsync);
    }

    public bool IsDisposed
    {
        get
        {
            lock (_sync)
            {
                return _disposed;
            }
        }
    }

    public Task RunAsync()
    {
        return _coalescingAction.RequestAsync();
    }

    public bool TryApply(Action apply)
    {
        ArgumentNullException.ThrowIfNull(apply);

        lock (_sync)
        {
            if (_disposed)
            {
                return false;
            }

            apply();
            return true;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _coalescingAction.Dispose();
    }

    private Task RunIfAliveAsync()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            return _refresh();
        }
    }
}
