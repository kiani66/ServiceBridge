using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Application.DTOs;

public class OrderFieldDefinition
{
    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public OrderFulfillmentMode FulfillmentMode { get; set; } =
        OrderFulfillmentMode.UseExistingAccount;
    public CredentialActor Actor { get; set; } = CredentialActor.Customer;
    public bool IsRequired { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsVisibleToCustomerAfterCompletion { get; set; }
    public int DisplayOrder { get; set; }
}
