namespace ServiceBridge.Application.Settings;

public class DevelopmentSeedSettings
{
    public const string SectionName = "DevelopmentSeed";

    public bool Enabled { get; set; }
    public decimal UsdRateToIrr { get; set; } = 850000m;
    public decimal GlobalMarginPercentage { get; set; } = 10m;
    public string TestUserEmail { get; set; } = string.Empty;
    public string TestAgentEmail { get; set; } = string.Empty;
}
