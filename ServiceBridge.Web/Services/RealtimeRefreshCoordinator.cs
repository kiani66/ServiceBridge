namespace ServiceBridge.Web.Services;

public sealed class RealtimeRefreshCoordinator
{
    private readonly object _sync = new();
    private Task? _activeRefresh;
    private bool _refreshRequested;

    public Task RunAsync(Func<Task> refresh)
    {
        ArgumentNullException.ThrowIfNull(refresh);

        lock (_sync)
        {
            _refreshRequested = true;
            if (_activeRefresh is { IsCompleted: false })
            {
                return _activeRefresh;
            }

            _activeRefresh = RunPendingRefreshesAsync(refresh);
            return _activeRefresh;
        }
    }

    private async Task RunPendingRefreshesAsync(Func<Task> refresh)
    {
        while (true)
        {
            lock (_sync)
            {
                _refreshRequested = false;
            }

            await refresh();

            lock (_sync)
            {
                if (_refreshRequested)
                {
                    continue;
                }

                _activeRefresh = null;
                return;
            }
        }
    }
}
