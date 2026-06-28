using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class AddAgentOrderNoteRequest
{
    [Required]
    [MaxLength(2000)]
    public string Text { get; set; } = string.Empty;
    public bool IsVisibleToCustomer { get; set; }
}
