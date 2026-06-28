using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class AssignOrderRequest
{
    [Required(ErrorMessage = "Agent is required.")]
    public Guid AgentId { get; set; }

    [MaxLength(1000, ErrorMessage = "Assignment notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }
}
