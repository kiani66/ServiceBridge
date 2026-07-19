using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Entities;

public sealed class DeliveryArtifact
{
    public const int ProtectedValueMaxLength = 32768;
    public const int FingerprintLength = 64;

    private DeliveryArtifact()
    {
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid FulfillmentExecutionId { get; private set; }
    public Guid FulfillmentAttemptId { get; private set; }
    public DeliveryArtifactType ArtifactType { get; private set; }
    public string ProtectedValue { get; private set; } = string.Empty;
    public string Fingerprint { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public Order Order { get; private set; } = null!;
    public FulfillmentExecution FulfillmentExecution { get; private set; } = null!;
    public FulfillmentAttempt FulfillmentAttempt { get; private set; } = null!;

    public static DeliveryArtifact Create(
        Guid id,
        Guid orderId,
        Guid fulfillmentExecutionId,
        Guid fulfillmentAttemptId,
        DeliveryArtifactType artifactType,
        string protectedValue,
        string fingerprint,
        DateTime createdAtUtc)
    {
        EnsureIdentifier(id, nameof(id));
        EnsureIdentifier(orderId, nameof(orderId));
        EnsureIdentifier(fulfillmentExecutionId, nameof(fulfillmentExecutionId));
        EnsureIdentifier(fulfillmentAttemptId, nameof(fulfillmentAttemptId));

        if (!Enum.IsDefined(artifactType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(artifactType),
                artifactType,
                "Unsupported delivery artifact type.");
        }

        EnsureRequiredBoundedValue(
            protectedValue,
            ProtectedValueMaxLength,
            nameof(protectedValue));
        EnsureFingerprint(fingerprint);

        if (createdAtUtc == default || createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Creation timestamp must be a non-default UTC value.",
                nameof(createdAtUtc));
        }

        return new DeliveryArtifact
        {
            Id = id,
            OrderId = orderId,
            FulfillmentExecutionId = fulfillmentExecutionId,
            FulfillmentAttemptId = fulfillmentAttemptId,
            ArtifactType = artifactType,
            ProtectedValue = protectedValue,
            Fingerprint = fingerprint,
            CreatedAtUtc = createdAtUtc
        };
    }

    private static void EnsureIdentifier(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }
    }

    private static void EnsureRequiredBoundedValue(
        string value,
        int maximumLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        if (value.Length > maximumLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Value cannot exceed {maximumLength} characters.");
        }
    }

    private static void EnsureFingerprint(string fingerprint)
    {
        EnsureRequiredBoundedValue(
            fingerprint,
            FingerprintLength,
            nameof(fingerprint));

        if (fingerprint.Length != FingerprintLength ||
            fingerprint.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                "Fingerprint must be a 64-character hexadecimal value.",
                nameof(fingerprint));
        }
    }
}
