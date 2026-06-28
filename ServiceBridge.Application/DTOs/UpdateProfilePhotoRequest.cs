using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class UpdateProfilePhotoRequest
{
    [Required(ErrorMessage = "Profile image URL is required.")]
    [MaxLength(500, ErrorMessage = "Profile image URL cannot exceed 500 characters.")]
    [Url(ErrorMessage = "Please enter a valid URL.")]
    public string ProfileImageUrl { get; set; } = string.Empty;
}
