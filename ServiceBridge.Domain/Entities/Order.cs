using ServiceBridge.Domain.Enums;
using ServiceBridge.Domain.Exceptions;
using ServiceBridge.Domain.Orders;
using ServiceFulfillmentMode = ServiceBridge.Domain.Enums.FulfillmentMode;

namespace ServiceBridge.Domain.Entities;

public class Order : BaseEntity
{
    private readonly List<OrderStatusHistory> _statusHistory = new();
    private readonly List<AgentAssignment> _assignments = new();
    private readonly List<CustomerActionRequest> _customerActionRequests = new();

    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid? ServiceVariantId { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public OrderType OrderType { get; set; }
    public OrderFulfillmentMode FulfillmentMode { get; internal set; } =
        OrderFulfillmentMode.UseExistingAccount;
    public ServiceFulfillmentMode ServiceFulfillmentModeSnapshot { get; internal set; } =
        ServiceFulfillmentMode.ManualAgent;
    public OrderStatus Status { get; internal set; }
    public decimal ForeignPrice { get; internal set; }
    public string CurrencyCode { get; internal set; } = string.Empty;
    public decimal ExchangeRateSnapshot { get; internal set; }
    public decimal MarginPercentSnapshot { get; internal set; }
    public decimal BaseAmountIrr { get; internal set; }
    public decimal ProfitAmountIrr { get; internal set; }
    public decimal FinalAmountIrr { get; internal set; }
    public string? VariantNameSnapshot { get; internal set; }
    public int? VariantDurationDaysSnapshot { get; internal set; }
    public string? VariantRegionCodeSnapshot { get; internal set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public User User { get; set; } = null!;
    public DigitalService Service { get; set; } = null!;
    public ServiceVariant? ServiceVariant { get; set; }
    public User? AssignedAgent { get; set; }
    public ICollection<Payment> PaymentRecords { get; set; } = new List<Payment>();
    public ICollection<OrderFieldValue> FieldValues { get; set; } =
        new List<OrderFieldValue>();
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();
    public IReadOnlyCollection<AgentAssignment> Assignments => _assignments.AsReadOnly();
    public IReadOnlyCollection<CustomerActionRequest> CustomerActionRequests =>
        _customerActionRequests.AsReadOnly();
    public ICollection<OrderNote> Notes { get; set; } = new List<OrderNote>();
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public bool CanTransitionTo(OrderStatus newStatus)
    {
        return OrderStatusTransitions.CanTransition(Status, newStatus);
    }

    public void TransitionTo(
        OrderStatus newStatus,
        Guid changedByUserId,
        DateTime changedAtUtc,
        string? note = null)
    {
        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOrderTransitionException(Status, newStatus);
        }

        var previousStatus = Status;
        Status = newStatus;
        UpdatedAt = changedAtUtc;

        switch (newStatus)
        {
            case OrderStatus.Paid:
                PaidAt = changedAtUtc;
                break;
            case OrderStatus.Assigned:
                AssignedAt = changedAtUtc;
                break;
            case OrderStatus.Completed:
                CompletedAt = changedAtUtc;
                break;
            case OrderStatus.Cancelled:
                CancelledAt = changedAtUtc;
                break;
        }

        _statusHistory.Add(new OrderStatusHistory
        {
            OrderId = Id,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ActorType = OrderLogActorType.User,
            ActorId = changedByUserId,
            ChangedByUserId = changedByUserId,
            Note = note,
            CreatedAt = changedAtUtc
        });
    }

    public void MoveToWaitingForPayment(Guid userId, DateTime changedAtUtc)
    {
        TransitionTo(
            OrderStatus.WaitingForPayment,
            userId,
            changedAtUtc,
            "Order is waiting for payment.");
    }

    public void MarkPaid(Guid paymentId, string note)
    {
        if (Status != OrderStatus.WaitingForPayment)
        {
            throw new InvalidOrderTransitionException(Status, OrderStatus.Paid);
        }

        var paidAtUtc = DateTime.UtcNow;
        var previousStatus = Status;
        Status = OrderStatus.Paid;
        PaidAt = paidAtUtc;
        UpdatedAt = paidAtUtc;

        _statusHistory.Add(new OrderStatusHistory
        {
            OrderId = Id,
            PreviousStatus = previousStatus,
            NewStatus = OrderStatus.Paid,
            ActorType = OrderLogActorType.System,
            ActorId = paymentId,
            ChangedByUserId = null,
            Note = note,
            CreatedAt = paidAtUtc
        });
    }

    public void AssignToAgent(
        Guid agentId,
        Guid adminId,
        string? assignmentNotes,
        string statusNote)
    {
        if (Status != OrderStatus.Paid || AssignedAgentId.HasValue)
        {
            throw new InvalidOrderTransitionException(Status, OrderStatus.Assigned);
        }

        var assignedAtUtc = DateTime.UtcNow;
        var previousStatus = Status;
        Status = OrderStatus.Assigned;
        AssignedAgentId = agentId;
        AssignedAt = assignedAtUtc;
        UpdatedAt = assignedAtUtc;

        _assignments.Add(new AgentAssignment
        {
            OrderId = Id,
            AgentId = agentId,
            AssignedByAdminId = adminId,
            AssignedAt = assignedAtUtc,
            Notes = string.IsNullOrWhiteSpace(assignmentNotes)
                ? null
                : assignmentNotes.Trim(),
            CreatedAt = assignedAtUtc
        });

        _statusHistory.Add(new OrderStatusHistory
        {
            OrderId = Id,
            PreviousStatus = previousStatus,
            NewStatus = OrderStatus.Assigned,
            ActorType = OrderLogActorType.Admin,
            ActorId = adminId,
            ChangedByUserId = adminId,
            Note = statusNote,
            CreatedAt = assignedAtUtc
        });
    }

    public void StartProcessing(Guid agentId, string? note = null)
    {
        TransitionByAgent(agentId, OrderStatus.Processing, note);
    }

    public void StartInventoryFulfillment(Guid systemActorId, string? note = null)
    {
        TransitionForInventoryFulfillment(
            OrderStatus.Paid,
            OrderStatus.Processing,
            systemActorId,
            note);
    }

    public void CompleteInventoryFulfillment(Guid systemActorId, string? note = null)
    {
        TransitionForInventoryFulfillment(
            OrderStatus.Processing,
            OrderStatus.Completed,
            systemActorId,
            note);
    }

    public void RequestCustomerAction(Guid agentId, string note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            throw new InvalidOperationException(
                "A customer-facing action note is required.");
        }

        EnsureAssignedAgent(agentId);
        if (Status != OrderStatus.Processing)
        {
            throw new InvalidOrderTransitionException(
                Status,
                OrderStatus.Processing);
        }

        if (_customerActionRequests.Any(x => !x.IsResolved))
        {
            throw new InvalidOperationException(
                "An unresolved customer action request already exists.");
        }

        var requestedAtUtc = DateTime.UtcNow;
        var customerNote = note.Trim();
        _customerActionRequests.Add(new CustomerActionRequest
        {
            OrderId = Id,
            RequestedByAgentId = agentId,
            RequestText = customerNote,
            RequestedAt = requestedAtUtc,
            CreatedAt = requestedAtUtc
        });

        Notes.Add(new OrderNote
        {
            OrderId = Id,
            AuthorId = agentId,
            Text = customerNote,
            IsVisibleToCustomer = true,
            CreatedAt = requestedAtUtc
        });
        UpdatedAt = requestedAtUtc;
    }

    public void AcknowledgeCustomerResponse(Guid agentId, string? note = null)
    {
        EnsureAssignedAgent(agentId);
        if (Status != OrderStatus.Processing ||
            _customerActionRequests.Any(x => !x.IsResolved))
        {
            throw new InvalidOperationException(
                "Customer action is still pending.");
        }

        if (!string.IsNullOrWhiteSpace(note))
        {
            AddAgentNote(agentId, note, false);
        }
    }

    public void Complete(Guid agentId, string? note = null)
    {
        if (_customerActionRequests.Any(x => !x.IsResolved))
        {
            throw new InvalidOperationException(
                "Customer action is still pending.");
        }

        TransitionByAgent(agentId, OrderStatus.Completed, note);
    }

    public void Fail(Guid agentId, string failureReason)
    {
        if (string.IsNullOrWhiteSpace(failureReason))
        {
            throw new InvalidOperationException("Failure reason is required.");
        }

        TransitionByAgent(agentId, OrderStatus.Failed, failureReason.Trim());
    }

    public void RespondToCustomerAction(
        Guid userId,
        Guid requestId,
        string response)
    {
        if (UserId != userId)
        {
            throw new InvalidOperationException(
                "Only the order customer can respond to this request.");
        }

        var request = _customerActionRequests.SingleOrDefault(x => x.Id == requestId)
            ?? throw new InvalidOperationException(
                "Customer action request was not found.");
        var respondedAtUtc = DateTime.UtcNow;
        request.Respond(response, respondedAtUtc);

        Notes.Add(new OrderNote
        {
            OrderId = Id,
            AuthorId = userId,
            Text = request.CustomerResponse!,
            IsVisibleToCustomer = true,
            CreatedAt = respondedAtUtc
        });
        UpdatedAt = respondedAtUtc;
    }

    public void AddAgentNote(
        Guid agentId,
        string text,
        bool isVisibleToCustomer)
    {
        EnsureAssignedAgent(agentId);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Order note cannot be empty.");
        }

        var createdAtUtc = DateTime.UtcNow;
        Notes.Add(new OrderNote
        {
            OrderId = Id,
            AuthorId = agentId,
            Text = text.Trim(),
            IsVisibleToCustomer = isVisibleToCustomer,
            CreatedAt = createdAtUtc
        });
        UpdatedAt = createdAtUtc;
    }

    internal void RecordInitialStatus(
        Guid changedByUserId,
        DateTime changedAtUtc,
        string? note)
    {
        if (_statusHistory.Count > 0)
        {
            throw new InvalidOperationException("Initial order status has already been recorded.");
        }

        _statusHistory.Add(new OrderStatusHistory
        {
            OrderId = Id,
            PreviousStatus = null,
            NewStatus = Status,
            ActorType = OrderLogActorType.User,
            ActorId = changedByUserId,
            ChangedByUserId = changedByUserId,
            Note = note,
            CreatedAt = changedAtUtc
        });
    }

    private void TransitionByAgent(
        Guid agentId,
        OrderStatus newStatus,
        string? note)
    {
        EnsureAssignedAgent(agentId);

        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOrderTransitionException(Status, newStatus);
        }

        var changedAtUtc = DateTime.UtcNow;
        var previousStatus = Status;
        Status = newStatus;
        UpdatedAt = changedAtUtc;

        if (newStatus == OrderStatus.Completed)
        {
            CompletedAt = changedAtUtc;
        }

        _statusHistory.Add(new OrderStatusHistory
        {
            OrderId = Id,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ActorType = OrderLogActorType.Agent,
            ActorId = agentId,
            ChangedByUserId = agentId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = changedAtUtc
        });
    }

    private void TransitionForInventoryFulfillment(
        OrderStatus requiredCurrentStatus,
        OrderStatus newStatus,
        Guid systemActorId,
        string? note)
    {
        if (ServiceFulfillmentModeSnapshot != ServiceFulfillmentMode.InventoryCode ||
            Status != requiredCurrentStatus)
        {
            throw new InvalidOrderTransitionException(Status, newStatus);
        }

        var changedAtUtc = DateTime.UtcNow;
        var previousStatus = Status;
        Status = newStatus;
        UpdatedAt = changedAtUtc;

        if (newStatus == OrderStatus.Completed)
        {
            CompletedAt = changedAtUtc;
        }

        _statusHistory.Add(new OrderStatusHistory
        {
            OrderId = Id,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ActorType = OrderLogActorType.System,
            ActorId = systemActorId,
            ChangedByUserId = null,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = changedAtUtc
        });
    }

    private void EnsureAssignedAgent(Guid agentId)
    {
        if (AssignedAgentId != agentId)
        {
            throw new InvalidOperationException(
                "Only the assigned agent can process this order.");
        }
    }
}
