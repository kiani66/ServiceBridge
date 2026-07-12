using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceBridge.Web.Services;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class RealtimeClientLifecycleTests
{
    [Fact]
    public async Task StartAsync_CalledRepeatedly_StartsOneConnectionOnce()
    {
        var connection = new FakeRealtimeConnection();
        var factoryCalls = 0;
        await using var client = CreateClient(connection, () => factoryCalls++);

        await client.StartAsync();
        await client.StartAsync();

        Assert.Equal(1, factoryCalls);
        Assert.Equal(1, connection.StartCalls);
    }

    [Fact]
    public async Task StopAsync_IsIdempotent_AndSameConnectionCanRestart()
    {
        var connection = new FakeRealtimeConnection();
        var factoryCalls = 0;
        await using var client = CreateClient(connection, () => factoryCalls++);

        await client.StopAsync();
        await client.StartAsync();
        await client.StopAsync();
        await client.StopAsync();
        await client.StartAsync();

        Assert.Equal(1, factoryCalls);
        Assert.Equal(2, connection.StartCalls);
        Assert.Equal(1, connection.StopCalls);
    }

    [Fact]
    public async Task DisposeAsync_IsFinalAndReleasesConnectionOnce()
    {
        var connection = new FakeRealtimeConnection();
        var client = CreateClient(connection);

        await client.StartAsync();
        await client.DisposeAsync();
        await client.DisposeAsync();
        await client.StartAsync();

        Assert.Equal(1, connection.StartCalls);
        Assert.Equal(1, connection.DisposeCalls);
    }

    [Fact]
    public async Task StopAsync_WhenTransportStopFails_DoesNotBlockLogoutFlow()
    {
        var connection = new FakeRealtimeConnection { ThrowOnStop = true };
        await using var client = CreateClient(connection);

        await client.StartAsync();
        var exception = await Record.ExceptionAsync(() => client.StopAsync());

        Assert.Null(exception);
        Assert.Equal(1, connection.StopCalls);
    }

    private static RealtimeClient CreateClient(
        FakeRealtimeConnection connection,
        Action? onFactoryCalled = null)
    {
        return new RealtimeClient(
            NullLogger<RealtimeClient>.Instance,
            _ => Task.FromResult<string?>("test-access-token"),
            _ =>
            {
                onFactoryCalled?.Invoke();
                return connection;
            });
    }

    private sealed class FakeRealtimeConnection : IRealtimeConnection
    {
        public HubConnectionState State { get; private set; } =
            HubConnectionState.Disconnected;
        public int StartCalls { get; private set; }
        public int StopCalls { get; private set; }
        public int DisposeCalls { get; private set; }
        public bool ThrowOnStop { get; init; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartCalls++;
            State = HubConnectionState.Connected;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCalls++;
            if (ThrowOnStop)
            {
                throw new InvalidOperationException("Simulated stop failure.");
            }

            State = HubConnectionState.Disconnected;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            DisposeCalls++;
            State = HubConnectionState.Disconnected;
            return ValueTask.CompletedTask;
        }
    }
}
