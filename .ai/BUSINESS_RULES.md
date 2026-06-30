# Business Rules

ServiceBridge is a digital service mediation and payment platform, not an e-commerce store.

## Core Flow

User selects service/variant -> system calculates IRR price -> user submits order fields/credentials -> user pays -> payment is verified -> order becomes Paid -> admin assigns to Agent or fulfillment path -> Agent processes -> order completes -> user tracks timeline.

## Pricing

```text
ForeignPrice x ExchangeRate x (1 + MarginPercent / 100) = FinalPriceIRR
```

Pricing snapshots are immutable. An order must keep the exact pricing snapshot used at creation/payment time.

## Orders

- Domain validates order transitions.
- Order history must be append-only.
- Do not rewrite historical events to represent new state.
- Timeline data should be derived from recorded order history.

## Payments

- Payment callback data must be verified before confirming payment.
- Payment verification must happen before an order becomes `Paid`.
- Payment failures should be represented as expected business outcomes when possible.

## Credentials

- Credentials must be encrypted at rest.
- Credentials are fulfillment data, not general profile data.
- Only expose credentials to authorized fulfillment paths.
