using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class FailOrderRequest
{
    [Required(ErrorMessage = "Failure reason is required.")]
    [MaxLength(1000, ErrorMessage = "Failure reason cannot exceed 1000 characters.")]
    public string Reason { get; set; } = string.Empty;
}
