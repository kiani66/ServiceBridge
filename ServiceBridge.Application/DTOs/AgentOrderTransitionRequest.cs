using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class AgentOrderTransitionRequest
{
    [MaxLength(1000)]
    public string? Note { get; set; }
}
