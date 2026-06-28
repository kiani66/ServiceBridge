using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class RequestCustomerActionRequest
{
    [Required]
    [MaxLength(1000)]
    public string Note { get; set; } = string.Empty;
}
