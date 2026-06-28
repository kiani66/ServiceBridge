namespace ServiceBridge.Application.DTOs;

public class CustomerActionRequestDto
{
    public Guid Id { get; set; }
    public string RequestText { get; set; } = string.Empty;
    public string? CustomerResponse { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public bool IsResolved { get; set; }
}
