namespace ServiceBridge.Web.Services;

using System.Runtime.ExceptionServices;

public sealed class CoalescingAsyncAction : IDisposable
{
    private readonly Func<Task> _action;
    private readonly object _sync = new();
    private Task _activeTask = Task.CompletedTask;
    private bool _disposed;
    private bool _pending;
    private bool _running;

    public CoalescingAsyncAction(Func<Task> action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public Task RequestAsync()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            if (_running)
            {
                _pending = true;
                return _activeTask;
            }

            _running = true;
            _activeTask = RunAsync();
            return _activeTask;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            _pending = false;
        }
    }

    private async Task RunAsync()
    {
        ExceptionDispatchInfo? capturedFailure = null;

        while (true)
        {
            try
            {
                await _action();
            }
            catch (Exception exception)
            {
                capturedFailure ??= ExceptionDispatchInfo.Capture(exception);
            }

            lock (_sync)
            {
                if (_disposed || !_pending)
                {
                    _running = false;
                    _pending = false;
                    break;
                }

                _pending = false;
            }
        }

        capturedFailure?.Throw();
    }
}
