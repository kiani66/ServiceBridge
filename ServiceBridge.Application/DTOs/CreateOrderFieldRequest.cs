using System.ComponentModel.DataAnnotations;

namespace ServiceBridge.Application.DTOs;

public class CreateOrderFieldRequest
{
    [Required(ErrorMessage = "Field key is required.")]
    [MaxLength(100, ErrorMessage = "Field key cannot exceed 100 characters.")]
    public string FieldKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Field value is required.")]
    [MaxLength(2000, ErrorMessage = "Field value cannot exceed 2000 characters.")]
    public string Value { get; set; } = string.Empty;
}
