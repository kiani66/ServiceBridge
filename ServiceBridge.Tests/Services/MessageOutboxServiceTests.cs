using ServiceBridge.Application.DTOs;
using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using ServiceBridge.Infrastructure.Services;
using ServiceBridge.Shared.Constants;
using ServiceBridge.Tests.TestDoubles;
using Xunit;

namespace ServiceBridge.Tests.Services;

public class MessageOutboxServiceTests
{
    [Fact]
    public async Task EnqueueAsync_CreatesPendingMessage()
    {
        var repository = new FakeMessageOutboxRepository();
        var service = new MessageOutboxService(repository);
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        var result = await service.EnqueueAsync(new EnqueueMessageRequest
        {
            UserId = userId,
            Channel = MessageChannel.Email,
            Recipient = " user@example.com ",
            Subject = " Welcome ",
            Body = "Body",
            RelatedEntityType = "Order",
            RelatedEntityId = orderId
        });

        Assert.True(result.Success);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal("Pending", result.Data?.Status);
        Assert.Equal("Email", result.Data?.Channel);
        Assert.Equal("user@example.com", result.Data?.Recipient);
        Assert.Equal("Welcome", result.Data?.Subject);
        Assert.Equal(userId, result.Data?.UserId);
        Assert.Equal(orderId, result.Data?.RelatedEntityId);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task EnqueueAsync_WhenRecipientMissing_ReturnsValidationFailure()
    {
        var service = new MessageOutboxService(new FakeMessageOutboxRepository());

        var result = await service.EnqueueAsync(new EnqueueMessageRequest
        {
            Channel = MessageChannel.Sms,
            Recipient = " ",
            Body = "Body"
        });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.ValidationFailed, result.ErrorCode);
    }

    [Fact]
    public async Task MarkProcessingAsync_WhenPending_IncrementsAttemptCount()
    {
        var repository = new FakeMessageOutboxRepository();
        var message = CreateMessage();
        repository.Seed(message);
        var service = new MessageOutboxService(repository);

        var result = await service.MarkProcessingAsync(message.Id);

        Assert.True(result.Success);
        Assert.Equal("Processing", result.Data?.Status);
        Assert.Equal(1, result.Data?.AttemptCount);
    }

    [Fact]
    public async Task MarkFailedAsync_WhenProcessing_StoresFailureAndNextAttempt()
    {
        var repository = new FakeMessageOutboxRepository();
        var message = CreateMessage();
        message.MarkProcessing(DateTime.UtcNow);
        repository.Seed(message);
        var service = new MessageOutboxService(repository);
        var nextAttemptAt = DateTime.UtcNow.AddMinutes(5);

        var result = await service.MarkFailedAsync(
            message.Id,
            "Provider disabled",
            nextAttemptAt);

        Assert.True(result.Success);
        Assert.Equal("Failed", result.Data?.Status);
        Assert.Equal("Provider disabled", result.Data?.Error);
        Assert.Equal(nextAttemptAt, result.Data?.NextAttemptAt);
    }

    [Fact]
    public async Task MarkProcessingAsync_WhenFailed_AllowsRetry()
    {
        var repository = new FakeMessageOutboxRepository();
        var message = CreateMessage();
        message.MarkProcessing(DateTime.UtcNow);
        message.MarkFailed("Temporary failure", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(1));
        repository.Seed(message);
        var service = new MessageOutboxService(repository);

        var result = await service.MarkProcessingAsync(message.Id);

        Assert.True(result.Success);
        Assert.Equal("Processing", result.Data?.Status);
        Assert.Equal(2, result.Data?.AttemptCount);
        Assert.Null(result.Data?.Error);
        Assert.Null(result.Data?.NextAttemptAt);
    }

    [Fact]
    public async Task MarkFailedAsync_WhenSent_ReturnsConflict()
    {
        var repository = new FakeMessageOutboxRepository();
        var message = CreateMessage();
        message.MarkProcessing(DateTime.UtcNow);
        message.MarkSent(DateTime.UtcNow);
        repository.Seed(message);
        var service = new MessageOutboxService(repository);

        var result = await service.MarkFailedAsync(
            message.Id,
            "late failure",
            null);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(AppConstants.ErrorCodes.Conflict, result.ErrorCode);
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
}
