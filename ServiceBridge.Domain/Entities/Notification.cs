using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string RelatedEntityType { get; set; } = string.Empty;
    public Guid RelatedEntityId { get; set; }
    public string? LinkUrl { get; set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public User User { get; set; } = null!;

    public void MarkRead(DateTime readAtUtc)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAt = readAtUtc;
        UpdatedAt = readAtUtc;
    }
}
