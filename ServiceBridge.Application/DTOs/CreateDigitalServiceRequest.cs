using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class CreateDigitalServiceRequest
{
    [Required(ErrorMessage = "Category is required.")]
    public Guid CategoryId { get; set; }

    [Required(ErrorMessage = "Service name is required.")]
    [MaxLength(150, ErrorMessage = "Service name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Provider is required.")]
    [MaxLength(150, ErrorMessage = "Provider cannot exceed 150 characters.")]
    public string Provider { get; set; } = string.Empty;

    [Required(ErrorMessage = "Service slug is required.")]
    [MaxLength(170, ErrorMessage = "Service slug cannot exceed 170 characters.")]
    [RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Service slug must contain lowercase letters, numbers, and hyphens only.")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    [Url(ErrorMessage = "Logo URL must be a valid URL.")]
    [MaxLength(500, ErrorMessage = "Logo URL cannot exceed 500 characters.")]
    public string? LogoUrl { get; set; }

    [Required(ErrorMessage = "Currency code is required.")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must contain 3 characters.")]
    public string CurrencyCode { get; set; } = "USD";

    [Range(typeof(decimal), "0.0001", "99999999999999",
        ErrorMessage = "Base price must be greater than zero.")]
    public decimal BasePrice { get; set; }

    public bool IsActive { get; set; } = true;

    public bool SupportsUseExistingAccount { get; set; } = true;
    public bool SupportsCreateNewAccount { get; set; }

    [MaxLength(250, ErrorMessage = "Processing time description cannot exceed 250 characters.")]
    public string? ProcessingTimeDescription { get; set; }
}
