namespace ServiceBridge.Application.DTOs;

public class UserVisibleOrderNoteDto
{
    public string Text { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
