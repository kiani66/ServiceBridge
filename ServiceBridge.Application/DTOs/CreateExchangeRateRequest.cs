using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class CreateExchangeRateRequest
{
    [Required(ErrorMessage = "Currency code is required.")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must contain 3 characters.")]
    public string CurrencyCode { get; set; } = "USD";

    [Range(typeof(decimal), "0.0001", "99999999999999",
        ErrorMessage = "Exchange rate must be greater than zero.")]
    public decimal RateToIrr { get; set; }

    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Effective date is required.")]
    public DateTime EffectiveFromUtc { get; set; }
}
