namespace ServiceBridge.Application.DTOs;

public class AgentOrderNoteDto
{
    public string AuthorName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsVisibleToCustomer { get; set; }
    public DateTime CreatedAt { get; set; }
}
