namespace ServiceBridge.Application.Settings;

public sealed class DeliveryArtifactProtectionOptions
{
    public const string SectionName = "DeliveryArtifactProtection";

    public string? FingerprintKey { get; set; }
}
