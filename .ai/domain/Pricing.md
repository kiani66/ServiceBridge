# Pricing

## Formula

```text
ForeignPrice x ExchangeRate x (1 + MarginPercent / 100) = FinalPriceIRR
```

## Rules

- Pricing is variant-aware.
- Pricing snapshots are immutable.
- Orders must preserve the pricing snapshot used for the order.
- Later exchange-rate, margin, or variant-price changes must not rewrite historical order prices.

## Implementation Guidance

- Keep pricing calculation in the appropriate Application or Domain service boundary.
- Store enough snapshot data to explain the final IRR price later.
- Do not rely on current variant pricing to display historical order totals.
