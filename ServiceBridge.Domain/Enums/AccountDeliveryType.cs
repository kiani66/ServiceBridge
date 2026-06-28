namespace ServiceBridge.Domain.Enums;

/// <summary>
/// Describes what the customer or operator receives for account-oriented services.
/// This is distinct from the current order processing mode represented by <see cref="OrderFulfillmentMode"/>.
/// </summary>
public enum AccountDeliveryType
{
    NotApplicable = 0,
    UseExistingAccount = 1,
    CreateNewAccount = 2,
    ActivationCode = 3,
    GiftCardCode = 4,
    InstructionOnly = 5
}
