namespace ServiceBridge.Application.DTOs;

public class MessageOutboxProcessResult
{
    public int Picked { get; set; }
    public int Processed { get; set; }
    public int Sent { get; set; }
    public int Retried { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
}
