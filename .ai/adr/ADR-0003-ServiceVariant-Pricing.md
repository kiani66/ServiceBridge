# ADR-0003: Service Variant Pricing

## Status

Accepted.

## Decision

Pricing is variant-aware and uses immutable pricing snapshots for orders.

## Formula

```text
ForeignPrice x ExchangeRate x (1 + MarginPercent / 100) = FinalPriceIRR
```

## Consequences

- A selected service variant drives the calculated IRR price.
- Orders preserve the pricing snapshot used at creation/payment time.
- Historical order prices do not change when catalog pricing, exchange rates, or margins change later.
