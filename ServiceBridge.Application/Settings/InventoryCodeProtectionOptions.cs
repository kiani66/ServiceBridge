namespace ServiceBridge.Application.Settings;

public sealed class InventoryCodeProtectionOptions
{
    public const string SectionName = "InventoryCodeProtection";

    public string? FingerprintKey { get; set; }
}
