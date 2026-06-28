using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class RespondToCustomerActionRequest
{
    [Required(ErrorMessage = "Customer action request is required.")]
    public Guid RequestId { get; set; }

    [Required(ErrorMessage = "Response is required.")]
    [MaxLength(2000, ErrorMessage = "Response cannot exceed 2000 characters.")]
    public string Response { get; set; } = string.Empty;
}
