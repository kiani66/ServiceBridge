using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class CreateProfitMarginRequest : IValidatableObject
{
    public Guid? CategoryId { get; set; }
    public Guid? ServiceId { get; set; }

    [Range(typeof(decimal), "0", "100",
        ErrorMessage = "Margin percentage must be between 0 and 100.")]
    public decimal Percentage { get; set; }

    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Effective date is required.")]
    public DateTime EffectiveFromUtc { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CategoryId.HasValue && ServiceId.HasValue)
        {
            yield return new ValidationResult(
                "Specify either a category or a service, not both.",
                new[] { nameof(CategoryId), nameof(ServiceId) });
        }
    }
}
