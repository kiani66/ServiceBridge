using ServiceBridge.Domain.Entities;
using ServiceBridge.Domain.Enums;
using Xunit;

namespace ServiceBridge.Tests.DeliveryArtifacts;

public sealed class DeliveryArtifactTests
{
    [Fact]
    public void Create_WithValidProtectedValues_CreatesImmutableArtifact()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var createdAtUtc = new DateTime(2026, 7, 19, 9, 0, 0, DateTimeKind.Utc);

        var artifact = DeliveryArtifact.Create(
            id,
            orderId,
            executionId,
            attemptId,
            DeliveryArtifactType.DigitalCode,
            "protected-value",
            new string('A', DeliveryArtifact.FingerprintLength),
            createdAtUtc);

        Assert.Equal(id, artifact.Id);
        Assert.Equal(orderId, artifact.OrderId);
        Assert.Equal(executionId, artifact.FulfillmentExecutionId);
        Assert.Equal(attemptId, artifact.FulfillmentAttemptId);
        Assert.Equal(DeliveryArtifactType.DigitalCode, artifact.ArtifactType);
        Assert.Equal("protected-value", artifact.ProtectedValue);
        Assert.Equal(createdAtUtc, artifact.CreatedAtUtc);
        Assert.All(
            typeof(DeliveryArtifact).GetProperties(),
            property => Assert.True(
                property.SetMethod is null || !property.SetMethod.IsPublic));
        Assert.Empty(
            typeof(DeliveryArtifact).GetMethods()
                .Where(method =>
                    method.IsPublic &&
                    !method.IsStatic &&
                    method.DeclaringType == typeof(DeliveryArtifact) &&
                    !method.IsSpecialName));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Create_WithEmptyIdentifier_Throws(int identifierIndex)
    {
        var identifiers = new[]
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };
        identifiers[identifierIndex] = Guid.Empty;

        Assert.Throws<ArgumentException>(() => DeliveryArtifact.Create(
            identifiers[0],
            identifiers[1],
            identifiers[2],
            identifiers[3],
            DeliveryArtifactType.GiftLink,
            "protected-value",
            new string('B', DeliveryArtifact.FingerprintLength),
            DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithUndefinedArtifactType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(
            artifactType: (DeliveryArtifactType)999));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-fingerprint")]
    public void Create_WithInvalidProtectedState_Throws(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Assert.ThrowsAny<ArgumentException>(() =>
                Create(protectedValue: value));
        }

        Assert.ThrowsAny<ArgumentException>(() => Create(fingerprint: value));
    }

    [Fact]
    public void Create_WithNonUtcTimestamp_Throws()
    {
        Assert.Throws<ArgumentException>(() => Create(
            createdAtUtc: DateTime.SpecifyKind(
                DateTime.UtcNow,
                DateTimeKind.Local)));
    }

    private static DeliveryArtifact Create(
        DeliveryArtifactType artifactType = DeliveryArtifactType.DigitalCode,
        string protectedValue = "protected-value",
        string? fingerprint = null,
        DateTime? createdAtUtc = null)
    {
        return DeliveryArtifact.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            artifactType,
            protectedValue,
            fingerprint ?? new string('C', DeliveryArtifact.FingerprintLength),
            createdAtUtc ?? DateTime.UtcNow);
    }
}
