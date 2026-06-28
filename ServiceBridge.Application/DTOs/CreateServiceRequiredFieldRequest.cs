using System.ComponentModel.DataAnnotations;
using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public class CreateServiceRequiredFieldRequest
{
    [Required(ErrorMessage = "Field key is required.")]
    [MaxLength(100, ErrorMessage = "Field key cannot exceed 100 characters.")]
    [RegularExpression("^[a-z][a-z0-9_]*$",
        ErrorMessage = "Field key must use lowercase letters, numbers, and underscores.")]
    public string FieldKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Field label is required.")]
    [MaxLength(150, ErrorMessage = "Field label cannot exceed 150 characters.")]
    public string Label { get; set; } = string.Empty;

    [EnumDataType(typeof(ServiceFieldType), ErrorMessage = "Invalid field type.")]
    public ServiceFieldType FieldType { get; set; }

    [EnumDataType(typeof(OrderFulfillmentMode), ErrorMessage = "Invalid fulfillment mode.")]
    public OrderFulfillmentMode FulfillmentMode { get; set; } =
        OrderFulfillmentMode.UseExistingAccount;

    [EnumDataType(typeof(CredentialActor), ErrorMessage = "Invalid credential actor.")]
    public CredentialActor Actor { get; set; } = CredentialActor.Customer;

    public bool IsRequired { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsVisibleToCustomerAfterCompletion { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Display order cannot be negative.")]
    public int DisplayOrder { get; set; }

    [MaxLength(2000, ErrorMessage = "Options JSON cannot exceed 2000 characters.")]
    public string? OptionsJson { get; set; }
}
