namespace ServiceBridge.Infrastructure.Services;

public class SmsOptions
{
    public const string SectionName = "Sms";

    public bool Enabled { get; set; } = false;
    public string Provider { get; set; } = "Http";
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
