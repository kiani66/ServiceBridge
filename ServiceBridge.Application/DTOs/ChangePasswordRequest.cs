using System.ComponentModel.DataAnnotations;
using ServiceBridge.Application.Validation;

namespace ServiceBridge.Application.DTOs;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [PasswordPolicy]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm new password is required.")]
    [Compare("NewPassword", ErrorMessage = "New passwords do not match.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
