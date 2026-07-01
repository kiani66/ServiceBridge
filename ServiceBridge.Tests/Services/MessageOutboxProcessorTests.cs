using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ServiceBridge.Application.Interfaces;
using ServiceBridge.Application.Results;
using ServiceBridge.Application.Settings;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class MessageOutboxProcessorTests
{
    [Fact]
    public async Task ProcessPendingAsync_WhenProviderSucceeds_MarksMessageSent()
    {
        var repository = new FakeMessageOutboxRepository();
        var message = CreateMessage();
        repository.Seed(message);
        var processor = CreateProcessor(repository, new FakeDeliveryProvider());

        var result = await processor.ProcessPendingAsync();

        Assert.True(result.Success);
        Assert.Equal(1, result.Data?.Picked);
        Assert.Equal(1, result.Data?.Processed);
        Assert.Equal(1, result.Data?.Sent);
        Assert.Equal(MessageStatus.Sent, message.Status);
        Assert.Equal(1, message.AttemptCount);
        Assert.NotNull(message.ProcessedAt);
        Assert.Null(message.NextAttemptAt);
        Assert.Equal(2, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenProviderFailsBelowMaxAttempts_RequeuesPendingMessage()
    {
        var repository = new FakeMessageOutboxRepository();
        var message = CreateMessage();
        repository.Seed(message);
        var processor = CreateProcessor(
            repository,
            new FakeDeliveryProvider(ServiceResult.Fail("Temporary provider failure", "ProviderFailure", 503)));

        var result = await processor.ProcessPendingAsync();

        Assert.True(result.Success);
        Assert.Equal(1, result.Data?.Retried);
        Assert.Equal(MessageStatus.Pending, message.Status);
        Assert.Equal(1, message.AttemptCount);
        Assert.Equal("Temporary provider failure", message.Error);
        Assert.NotNull(message.NextAttemptAt);
        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenProviderFailsAtMaxAttempts_MarksMessageFailed()
    {
        var repository = new FakeMessageOutboxRepository();
        var message = CreateMessage();
        message.MarkProcessing(DateTime.UtcNow.AddMinutes(-10));
        message.MarkPendingRetry(
            "Earlier failure",
            DateTime.UtcNow.AddMinutes(-10),
            DateTime.UtcNow.AddMinutes(-5));
        repository.Seed(message);
        var processor = CreateProcessor(
            repository,
            new FakeDeliveryProvider(ServiceResult.Fail("Final provider failure", "ProviderFailure", 503)),
            maxAttempts: 2);

        var result = await processor.ProcessPendingAsync();

        Assert.True(result.Success);
        Assert.Equal(1, result.Data?.Failed);
        Assert.Equal(MessageStatus.Failed, message.Status);
        Assert.Equal(2, message.AttemptCount);
        Assert.Equal("Final provider failure", message.Error);
        Assert.Null(message.NextAttemptAt);
        Assert.NotNull(message.ProcessedAt);
    }

    [Fact]
    public async Task ProcessPendingAsync_DoesNotProcessTerminalMessages()
    {
        var repository = new FakeMessageOutboxRepository();
        var sent = CreateMessage();
        sent.MarkProcessing(DateTime.UtcNow);
        sent.MarkSent(DateTime.UtcNow);
        var failed = CreateMessage();
        failed.MarkProcessing(DateTime.UtcNow);
        failed.MarkFailed("failed", DateTime.UtcNow, null);
        var cancelled = CreateMessage();
        cancelled.Cancel(DateTime.UtcNow, "cancelled");
        repository.Seed(sent);
        repository.Seed(failed);
        repository.Seed(cancelled);
        var provider = new FakeDeliveryProvider();
        var processor = CreateProcessor(repository, provider);

        var result = await processor.ProcessPendingAsync();

        Assert.True(result.Success);
        Assert.Equal(0, result.Data?.Picked);
        Assert.Equal(0, provider.CallCount);
    }

    [Fact]
    public async Task ProcessPendingAsync_WhenBatchContainsDuplicateMessage_ProcessesOnce()
    {
        var repository = new FakeMessageOutboxRepository();
        var message = CreateMessage();
        repository.Seed(message);
        repository.SeedDueOverride(message, message);
        var provider = new FakeDeliveryProvider();
        var processor = CreateProcessor(repository, provider);

        var result = await processor.ProcessPendingAsync();

        Assert.True(result.Success);
        Assert.Equal(2, result.Data?.Picked);
        Assert.Equal(1, result.Data?.Processed);
        Assert.Equal(1, result.Data?.Skipped);
        Assert.Equal(1, provider.CallCount);
    }

    [Fact]
    public void MessageOutboxSettings_DefaultsProcessorDisabled()
    {
        var settings = new MessageOutboxSettings();

        Assert.False(settings.Enabled);
        Assert.Equal(20, settings.BatchSize);
        Assert.Equal(30, settings.PollIntervalSeconds);
        Assert.Equal(5, settings.MaxAttempts);
    }

    private static MessageOutboxProcessor CreateProcessor(
        FakeMessageOutboxRepository repository,
        IMessageDeliveryProvider provider,
        int maxAttempts = 5)
    {
        return new MessageOutboxProcessor(
            repository,
            provider,
            Options.Create(new MessageOutboxSettings
            {
                Enabled = true,
                BatchSize = 20,
                PollIntervalSeconds = 30,
                MaxAttempts = maxAttempts
            }),
            NullLogger<MessageOutboxProcessor>.Instance);
    }

    private static MessageOutbox CreateMessage()
    {
        return MessageOutbox.Create(
            MessageChannel.Email,
            "user@example.com",
            "Subject",
            "Body",
            null,
            null,
            null,
            DateTime.UtcNow);
    }

    private sealed class FakeDeliveryProvider : IMessageDeliveryProvider
    {
        private readonly ServiceResult _result;

        public FakeDeliveryProvider()
            : this(ServiceResult.Ok())
        {
        }

        public FakeDeliveryProvider(ServiceResult result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public Task<ServiceResult> SendAsync(
            MessageOutbox message,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }
}
