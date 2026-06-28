using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class CreateServiceCategoryRequest
{
    [Required(ErrorMessage = "Category name is required.")]
    [MaxLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category slug is required.")]
    [MaxLength(120, ErrorMessage = "Category slug cannot exceed 120 characters.")]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Category slug must contain lowercase letters, numbers, and hyphens only.")]
    public string Slug { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Display order cannot be negative.")]
    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
