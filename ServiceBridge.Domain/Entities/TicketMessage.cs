namespace ServiceBridge.Domain.Entities;

public class TicketMessage : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public User Author { get; set; } = null!;

    public static TicketMessage Create(
        Guid ticketId,
        Guid authorId,
        string body,
        bool isInternal,
        DateTime createdAtUtc)
    {
        return new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorId = authorId,
            Body = body.Trim(),
            IsInternal = isInternal,
            CreatedAt = createdAtUtc
        };
    }
}
