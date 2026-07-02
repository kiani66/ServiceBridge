using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public class TicketListItemDto
{
    public Guid TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? AssignedAgentName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
}

public class TicketDetailDto : TicketListItemDto
{
    public Guid UserId { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public IReadOnlyCollection<TicketMessageDto> Messages { get; set; } =
        Array.Empty<TicketMessageDto>();
}

public class TicketMessageDto
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTicketRequest
{
    public Guid? OrderId { get; set; }
    public Guid? CategoryId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class CreateTicketMessageRequest
{
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
}

public class UpdateTicketPriorityRequest
{
    public TicketPriority Priority { get; set; }
}
