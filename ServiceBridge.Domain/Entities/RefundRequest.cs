using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Entities;

public class RefundRequest : BaseEntity
{
    private readonly List<RefundRequestStatusHistory> _statusHistory = new();

    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? ReviewedByAdminId { get; private set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; private set; } = RefundStatus.Requested;
    public DateTime? ReviewedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? AdminNote { get; private set; }

    public Order Order { get; set; } = null!;
    public Payment Payment { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
    public User? ReviewedByAdmin { get; set; }
    public IReadOnlyCollection<RefundRequestStatusHistory> StatusHistory =>
        _statusHistory.AsReadOnly();

    public static RefundRequest Create(
        Guid orderId,
        Guid paymentId,
        Guid requestedByUserId,
        decimal amount,
        string reason,
        DateTime createdAtUtc)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Refund amount must be positive.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Refund reason is required.");
        }

        var request = new RefundRequest
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            PaymentId = paymentId,
            RequestedByUserId = requestedByUserId,
            Amount = amount,
            Reason = reason.Trim(),
            Status = RefundStatus.Requested,
            CreatedAt = createdAtUtc
        };
        request.RecordStatusChange(
            null,
            RefundStatus.Requested,
            OrderLogActorType.User,
            requestedByUserId,
            "Refund requested.",
            createdAtUtc);

        return request;
    }

    public void Approve(Guid adminId, string? adminNote, DateTime reviewedAtUtc)
    {
        EnsureCurrentStatus(RefundStatus.Requested);
        ReviewedByAdminId = adminId;
        ReviewedAtUtc = reviewedAtUtc;
        AdminNote = NormalizeNote(adminNote);
        UpdatedAt = reviewedAtUtc;
        Status = RefundStatus.Approved;
        RecordStatusChange(
            RefundStatus.Requested,
            RefundStatus.Approved,
            OrderLogActorType.Admin,
            adminId,
            AdminNote ?? "Refund approved.",
            reviewedAtUtc);
    }

    public void Reject(Guid adminId, string adminNote, DateTime reviewedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(adminNote))
        {
            throw new InvalidOperationException("A rejection note is required.");
        }

        EnsureCurrentStatus(RefundStatus.Requested);
        ReviewedByAdminId = adminId;
        ReviewedAtUtc = reviewedAtUtc;
        AdminNote = adminNote.Trim();
        UpdatedAt = reviewedAtUtc;
        Status = RefundStatus.Rejected;
        RecordStatusChange(
            RefundStatus.Requested,
            RefundStatus.Rejected,
            OrderLogActorType.Admin,
            adminId,
            AdminNote,
            reviewedAtUtc);
    }

    public void Complete(Guid adminId, string? adminNote, DateTime completedAtUtc)
    {
        EnsureCurrentStatus(RefundStatus.Approved);
        CompletedAtUtc = completedAtUtc;
        AdminNote = NormalizeNote(adminNote) ?? AdminNote;
        UpdatedAt = completedAtUtc;
        Status = RefundStatus.Completed;
        RecordStatusChange(
            RefundStatus.Approved,
            RefundStatus.Completed,
            OrderLogActorType.Admin,
            adminId,
            AdminNote ?? "Refund completed.",
            completedAtUtc);
    }

    public void Cancel(Guid userId, DateTime cancelledAtUtc)
    {
        EnsureCurrentStatus(RefundStatus.Requested);
        UpdatedAt = cancelledAtUtc;
        Status = RefundStatus.Cancelled;
        RecordStatusChange(
            RefundStatus.Requested,
            RefundStatus.Cancelled,
            OrderLogActorType.User,
            userId,
            "Refund request cancelled.",
            cancelledAtUtc);
    }

    private void EnsureCurrentStatus(RefundStatus expectedStatus)
    {
        if (Status != expectedStatus)
        {
            throw new InvalidOperationException(
                "This refund request cannot move to the requested status.");
        }
    }

    private void RecordStatusChange(
        RefundStatus? previousStatus,
        RefundStatus newStatus,
        OrderLogActorType actorType,
        Guid actorId,
        string note,
        DateTime createdAtUtc)
    {
        _statusHistory.Add(new RefundRequestStatusHistory
        {
            RefundRequestId = Id,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ActorType = actorType,
            ActorId = actorId,
            Note = note,
            CreatedAt = createdAtUtc
        });
    }

    private static string? NormalizeNote(string? note)
    {
        return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
