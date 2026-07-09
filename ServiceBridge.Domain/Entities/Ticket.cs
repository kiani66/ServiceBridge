using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Entities;

public class Ticket : BaseEntity
{
    private readonly List<TicketMessage> _messages = new();

    public string TicketNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public TicketStatus Status { get; private set; } = TicketStatus.WaitingForSupport;
    public TicketPriority Priority { get; private set; } = TicketPriority.Normal;
    public DateTime LastActivityAt { get; private set; } = DateTime.UtcNow;
    public DateTime? FirstResponseAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public User User { get; set; } = null!;
    public Order? Order { get; set; }
    public TicketCategory? Category { get; set; }
    public User? AssignedAgent { get; set; }
    public IReadOnlyCollection<TicketMessage> Messages => _messages.AsReadOnly();
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public static Ticket Create(
        Guid userId,
        string ticketNumber,
        string subject,
        string body,
        Guid? orderId,
        Guid? categoryId,
        DateTime createdAtUtc)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TicketNumber = ticketNumber,
            Subject = subject.Trim(),
            OrderId = orderId,
            CategoryId = categoryId,
            CreatedAt = createdAtUtc,
            LastActivityAt = createdAtUtc
        };

        ticket._messages.Add(TicketMessage.Create(
            ticket.Id,
            userId,
            body,
            false,
            createdAtUtc));

        return ticket;
    }

    public TicketMessage AddUserReply(
        Guid userId,
        string body,
        DateTime createdAtUtc)
    {
        if (UserId != userId)
        {
            throw new InvalidOperationException(
                "Only the ticket owner can reply to this ticket.");
        }

        if (Status == TicketStatus.Closed)
        {
            throw new InvalidOperationException(
                "This ticket can no longer receive customer replies.");
        }

        var message = TicketMessage.Create(
            Id,
            userId,
            body,
            false,
            createdAtUtc);

        _messages.Add(message);
        Status = TicketStatus.WaitingForSupport;
        LastActivityAt = createdAtUtc;
        UpdatedAt = createdAtUtc;
        return message;
    }

    public TicketMessage AddAdminMessage(
        Guid adminId,
        string body,
        bool isInternal,
        DateTime createdAtUtc)
    {
        if (Status == TicketStatus.Closed)
        {
            throw new InvalidOperationException(
                "Closed tickets cannot receive new messages.");
        }

        var message = TicketMessage.Create(
            Id,
            adminId,
            body,
            isInternal,
            createdAtUtc);

        _messages.Add(message);
        LastActivityAt = createdAtUtc;
        UpdatedAt = createdAtUtc;

        if (!isInternal)
        {
            Status = TicketStatus.WaitingForUser;
            FirstResponseAt ??= createdAtUtc;
        }

        return message;
    }

    public void Close(Guid adminId, DateTime closedAtUtc)
    {
        if (Status == TicketStatus.Closed)
        {
            return;
        }

        Status = TicketStatus.Closed;
        ClosedAt = closedAtUtc;
        LastActivityAt = closedAtUtc;
        UpdatedAt = closedAtUtc;

        _messages.Add(TicketMessage.Create(
            Id,
            adminId,
            "Ticket closed by support.",
            true,
            closedAtUtc));
    }

    public void UpdatePriority(TicketPriority priority, DateTime updatedAtUtc)
    {
        Priority = priority;
        UpdatedAt = updatedAtUtc;
    }
}
