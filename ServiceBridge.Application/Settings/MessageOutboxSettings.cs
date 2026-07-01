namespace ServiceBridge.Application.Settings;

public class MessageOutboxSettings
{
    public const string SectionName = "MessageOutbox";

    public bool Enabled { get; set; } = false;
    public int BatchSize { get; set; } = 20;
    public int PollIntervalSeconds { get; set; } = 30;
    public int MaxAttempts { get; set; } = 5;
}
