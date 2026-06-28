using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Entities;

public class ServiceRequiredField : BaseEntity
{
    public Guid ServiceId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public ServiceFieldType FieldType { get; set; }
    public OrderFulfillmentMode FulfillmentMode { get; set; } =
        OrderFulfillmentMode.UseExistingAccount;
    public CredentialActor Actor { get; set; } = CredentialActor.Customer;
    public bool IsRequired { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsVisibleToCustomerAfterCompletion { get; set; }
    public int DisplayOrder { get; set; }
    public string? OptionsJson { get; set; }

    public DigitalService Service { get; set; } = null!;
}
