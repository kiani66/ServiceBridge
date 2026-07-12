using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceBridge.Application.DTOs;
using ServiceBridge.Web.Services;
using Xunit;

namespace ServiceBridge.Tests.Web;

public class RealtimeSubscriptionTests
{
    [Fact]
    public async Task SubscribeAndDispatch_InvokesActiveHandler()
    {
        var context = await CreateStartedClientAsync();
        await using var client = context.Client;
        var calls = 0;
        using var subscription = client.SubscribeToOrderUpdates(_ =>
        {
            calls++;
            return Task.CompletedTask;
        });

        await context.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto());

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task DisposeBeforeDispatch_PreventsInvocation_AndIsIdempotent()
    {
        var context = await CreateStartedClientAsync();
        await using var client = context.Client;
        var calls = 0;
        var subscription = client.SubscribeToOrderUpdates(_ =>
        {
            calls++;
            return Task.CompletedTask;
        });

        subscription.Dispose();
        subscription.Dispose();
        await context.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto());

        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task DisposeAfterSnapshotBeforeInvocation_SkipsDisposedRegistration()
    {
        var context = await CreateStartedClientAsync();
        await using var client = context.Client;
        var firstStarted = NewSignal();
        var releaseFirst = NewSignal();
        var secondCalls = 0;

        using var first = client.SubscribeToOrderUpdates(async _ =>
        {
            firstStarted.TrySetResult();
            await releaseFirst.Task;
        });
        var second = client.SubscribeToOrderUpdates(_ =>
        {
            secondCalls++;
            return Task.CompletedTask;
        });

        var dispatch = context.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto());
        await firstStarted.Task;
        second.Dispose();
        releaseFirst.TrySetResult();
        await dispatch;

        Assert.Equal(0, secondCalls);
    }

    [Fact]
    public async Task HandlerCanDisposeItself()
    {
        var context = await CreateStartedClientAsync();
        await using var client = context.Client;
        var calls = 0;
        IDisposable? subscription = null;
        subscription = client.SubscribeToOrderUpdates(_ =>
        {
            calls++;
            subscription!.Dispose();
            return Task.CompletedTask;
        });

        await context.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto());
        await context.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto());

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task FailingHandler_DoesNotPreventNextActiveHandler()
    {
        var context = await CreateStartedClientAsync();
        await using var client = context.Client;
        var successfulCalls = 0;
        using var failing = client.SubscribeToOrderUpdates(_ =>
            throw new InvalidOperationException("Simulated handler failure."));
        using var succeeding = client.SubscribeToOrderUpdates(_ =>
        {
            successfulCalls++;
            return Task.CompletedTask;
        });

        await context.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto());

        Assert.Equal(1, successfulCalls);
    }

    [Fact]
    public async Task DisposingComponentSubscriptionCollection_PreventsLaterInvocation()
    {
        var context = await CreateStartedClientAsync();
        await using var client = context.Client;
        var calls = 0;
        var subscriptions = new RealtimeSubscriptionCollection();
        subscriptions.Add(client.SubscribeToOrderUpdates(_ =>
        {
            calls++;
            return Task.CompletedTask;
        }));

        subscriptions.Dispose();
        subscriptions.Dispose();
        await context.Handlers.OrderUpdated(new OrderUpdatedRealtimeDto());

        Assert.Equal(0, calls);
    }

    private static async Task<TestContext> CreateStartedClientAsync()
    {
        RealtimeConnectionHandlers? handlers = null;
        var client = new RealtimeClient(
            NullLogger<RealtimeClient>.Instance,
            _ => Task.FromResult<string?>("test-access-token"),
            value =>
            {
                handlers = value;
                return new FakeRealtimeConnection();
            });

        await client.StartAsync();
        return new TestContext(client, handlers!);
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record TestContext(
        RealtimeClient Client,
        RealtimeConnectionHandlers Handlers);

    private sealed class FakeRealtimeConnection : IRealtimeConnection
    {
        public HubConnectionState State { get; private set; } = HubConnectionState.Disconnected;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            State = HubConnectionState.Connected;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            State = HubConnectionState.Disconnected;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            State = HubConnectionState.Disconnected;
            return ValueTask.CompletedTask;
        }
    }
}
