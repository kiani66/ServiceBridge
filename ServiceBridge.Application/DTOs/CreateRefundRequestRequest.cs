using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class CreateRefundRequestRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}
