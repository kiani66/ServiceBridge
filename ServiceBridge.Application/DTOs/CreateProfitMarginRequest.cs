using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class CreateProfitMarginRequest : IValidatableObject
{
    public Guid? ServiceVariantId { get; set; }
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
        var scopeCount = new[]
        {
            ServiceVariantId,
            ServiceId,
            CategoryId
        }.Count(x => x.HasValue);

        if (scopeCount > 1)
        {
            yield return new ValidationResult(
                "Specify a service variant, service or category, not more than one.",
                new[] { nameof(ServiceVariantId), nameof(ServiceId), nameof(CategoryId) });
        }
    }
}
