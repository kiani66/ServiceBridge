# ServiceBridge Order Creation

## Variant-based order creation

Orders are created from `ServiceVariant`.

`CreateOrderRequest.ServiceVariantId` is the canonical selected purchasable item.
The legacy `PlanId` request field remains supported as a compatibility alias
until the public contract is cleaned up in a later PR. Existing `/plans` routes
and `Plans` response properties remain compatible.

The order creation command validates the selected variant before creating an
order:

- the variant exists
- the variant is active
- the parent digital service is active
- the service category is active
- the provider is active

Business validation returns `ServiceResult` failures. The order creation flow
does not throw generic exceptions for expected catalog availability failures.

## Pricing snapshots

At creation time, the selected variant supplies the foreign price, currency and
region. Pricing then resolves the active exchange rate by variant currency and
the first active margin in this order: variant, service, category, global.

The order snapshots these values:

- `ServiceVariantId`
- `ForeignPrice`
- `CurrencyCode`
- `VariantRegionCodeSnapshot`
- `ExchangeRateSnapshot`
- `MarginPercentSnapshot`
- `BaseAmountIrr`
- `ProfitAmountIrr`
- `FinalAmountIrr`

These snapshots are immutable after order creation. Later changes to variant
price, exchange rates or margins must not alter existing orders, payments or
customer-visible order history.
