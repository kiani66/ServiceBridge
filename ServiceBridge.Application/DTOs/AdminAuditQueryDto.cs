using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public class AdminAuditQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string? Actor { get; set; }
    public string? Order { get; set; }
    public Guid? OrderId { get; set; }
    public string? EventType { get; set; }
    public OrderStatus? Status { get; set; }
}
