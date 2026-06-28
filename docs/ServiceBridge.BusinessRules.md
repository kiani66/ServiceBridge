# ServiceBridge Business Rules

Source: ServiceBridge Project Guideline v1.3.

This file records the highest-priority product rules that future PRs must preserve. PR 1 does not implement these modules.

## Pricing

- FinalPriceIRR = BaseForeignPrice * ExchangeRate * (1 + MarginPercent / 100).
- Pricing scope must evolve to ServiceVariant > DigitalService > Category > Global.
- Order creation must snapshot foreign price, currency code, exchange rate, margin percent, and final IRR amount.
- Future pricing changes must not mutate existing orders.
- ServiceBridge v1.0 active purchasable variants use USD to IRR pricing.

## Catalog

- ServiceVariant is the purchasable item for v1.0.
- RegionCode is informational and operational in v1.0; pricing uses CurrencyCode and BasePrice.
- Required fields define dynamic order forms and credential collection.
- Encrypted fields must never be stored or logged as plain text.

## Orders

- OrderNumber format is SB-yyyyMMdd-000000.
- OrderNumber is generated server-side only and is immutable.
- Order history is append-only.
- Domain must validate order status transitions.
- Only assigned Agents can process assigned orders.

## Payments and Refunds

- Payment callbacks must be verified before confirming payment.
- Duplicate callbacks must be idempotent.
- Refund in v1.0 is manual/external by default.
- Refund confirmation must record reference, amount, timestamp, Admin, note, PaymentEvent, OrderStatusHistory, and AuditLog.

## Audit and Security

- Admin, Agent, finance-sensitive, credential-sensitive, and security-sensitive actions must write AuditLog.
- AuditLog is append-only.
- Never log decrypted credentials, password hashes, or payment gateway secrets.
- Credential view auditing must record who viewed credentials and for which order, but not the credential values.

## Testing

- Unit tests must lock business rules.
- Critical HTTP, EF Core, SQL constraint, transaction, encryption, authentication, and concurrency behavior needs integration tests.
- EF Core InMemory must not be used for payment, encryption, order number, audit, or concurrency tests.
