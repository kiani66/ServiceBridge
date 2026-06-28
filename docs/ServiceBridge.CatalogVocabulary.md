# ServiceBridge Catalog Vocabulary

## Purpose

Catalog and pricing concepts affect filtering, pricing, fulfillment, validation, reporting, and user/operator workflow. Raw strings are still needed at API and persistence boundaries, but business-significant concepts should have structured domain vocabulary so later catalog work does not spread arbitrary string comparisons through the codebase.

This PR adds vocabulary only. It does not change runtime catalog behavior, order creation, pricing resolution, UI flows, or database schema.

## CurrencyCode

`CurrencyCode` is a Domain enum for supported ISO-style currencies:

- `Unknown = 0`
- `USD = 1`
- `EUR = 2`
- `GBP = 3`
- `AED = 4`
- `TRY = 5`

Existing string columns remain unchanged for compatibility. `CurrencyCodeExtensions` provides safe conversion between enum values and ISO strings for future domain logic.

## RegionCode

`RegionCode` is a lightweight value object. It normalizes codes to uppercase, rejects empty values, and keeps compatibility with existing string `RegionCode` columns.

Known values:

- `GLOBAL`
- `US`
- `UK`
- `TR`
- `AE`
- `EU`
- `DE`
- `FR`

This is intentionally not a large country enum. Unknown non-empty region codes can still be represented, which keeps the model extensible for future catalog imports or provider-specific regions.

## BillingCycle

`BillingCycle` describes how a plan or later variant is billed:

- `None`
- `OneTime`
- `Monthly`
- `Quarterly`
- `SemiAnnual`
- `Annual`
- `CustomDays`

Examples: one-time gift card purchases, monthly premium accounts, annual software licenses, or custom-day subscriptions.

## AccountDeliveryType

`AccountDeliveryType` describes what the customer/operator receives:

- `NotApplicable`
- `UseExistingAccount`
- `CreateNewAccount`
- `ActivationCode`
- `GiftCardCode`
- `InstructionOnly`

This is distinct from the current `OrderFulfillmentMode`, which is already used by order processing for existing-account and new-account flows.

## FulfillmentMode

`FulfillmentMode` describes the fulfillment channel:

- `ManualAgent`
- `InventoryCode`
- `ExternalAutomation`
- `InstructionOnly`

`ExternalAutomation` is future-ready only. No automation behavior is implemented by this PR.
