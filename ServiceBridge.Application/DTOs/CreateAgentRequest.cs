using System.ComponentModel.DataAnnotations;
using ServiceBridge.Application.Validation;

namespace ServiceBridge.Application.DTOs;

public class CreateAgentRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(150, ErrorMessage = "Full name cannot exceed 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [PasswordPolicy]
    public string Password { get; set; } = string.Empty;
}
