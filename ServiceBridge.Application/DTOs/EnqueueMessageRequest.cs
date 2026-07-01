using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public class EnqueueMessageRequest
{
    public Guid? UserId { get; set; }
    public MessageChannel Channel { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}
