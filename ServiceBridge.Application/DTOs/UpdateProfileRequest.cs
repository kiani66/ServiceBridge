using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
    public string FullName { get; set; } = string.Empty;
}
