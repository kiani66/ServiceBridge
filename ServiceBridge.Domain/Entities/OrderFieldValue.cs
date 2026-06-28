using ServiceBridge.Domain.Enums;

namespace ServiceBridge.Domain.Entities;

public class OrderFieldValue : BaseEntity
{
    public Guid OrderId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public OrderFulfillmentMode FulfillmentMode { get; set; } =
        OrderFulfillmentMode.UseExistingAccount;
    public CredentialActor Actor { get; set; } = CredentialActor.Customer;
    public string EncryptedValue { get; set; } = string.Empty;
    public bool IsSensitive { get; set; }
    public bool IsVisibleToCustomerAfterCompletion { get; set; }
    public int DisplayOrder { get; set; }

    public Order Order { get; set; } = null!;
}
