using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class CreateServiceVariantRequest
{
    [Required(ErrorMessage = "Variant name is required.")]
    [MaxLength(150, ErrorMessage = "Variant name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Variant slug is required.")]
    [MaxLength(170, ErrorMessage = "Variant slug cannot exceed 170 characters.")]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Variant slug must contain lowercase letters, numbers, and hyphens only.")]
    public string Slug { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Duration must be greater than zero.")]
    public int? DurationDays { get; set; }

    [MaxLength(20, ErrorMessage = "Region code cannot exceed 20 characters.")]
    public string? RegionCode { get; set; }

    [Required(ErrorMessage = "Currency code is required.")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must contain 3 characters.")]
    public string CurrencyCode { get; set; } = "USD";

    [Range(typeof(decimal), "0.0001", "99999999999999",
        ErrorMessage = "Base price must be greater than zero.")]
    public decimal BasePrice { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(0, int.MaxValue, ErrorMessage = "Display order cannot be negative.")]
    public int DisplayOrder { get; set; }

    [MaxLength(250, ErrorMessage = "Processing time description cannot exceed 250 characters.")]
    public string? ProcessingTimeDescription { get; set; }
}
