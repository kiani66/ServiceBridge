# ServiceBridge Pricing

## Resolution hierarchy

Pricing resolves from the selected `ServiceVariant` and uses the first active
configuration that is effective for the pricing time.

Margin priority:

1. `ServiceVariant`
2. `DigitalService`
3. `ServiceCategory`
4. Global

`ServiceVariant` is first because it is the purchasable catalog item. A variant
can have a distinct price, currency and region, so its margin must be able to
override broader service, category and global defaults.

## Exchange rates

Exchange rates resolve by the selected variant's `CurrencyCode`. ServiceBridge
v1.0 checkout supports USD to IRR, but the resolver still uses the stored
currency code instead of hardcoding USD.

## Immutable snapshots

Order creation stores the resolved foreign price, currency, region, exchange
rate, margin and IRR amounts on the order. Later changes to catalog prices,
exchange rates or margins must not mutate existing orders.
