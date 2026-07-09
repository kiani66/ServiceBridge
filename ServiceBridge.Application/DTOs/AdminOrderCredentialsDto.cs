namespace ServiceBridge.Application.DTOs;

public class AdminOrderCredentialsDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public IReadOnlyCollection<AgentOrderFieldDto> Credentials { get; set; } =
        Array.Empty<AgentOrderFieldDto>();
}
