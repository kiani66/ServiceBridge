# Orders

## Main Flow

User selects service/variant -> system calculates IRR price -> user submits order fields/credentials -> user pays -> payment is verified -> order becomes Paid -> admin assigns to Agent or fulfillment path -> Agent processes -> order completes -> user tracks timeline.

## Rules

- Domain validates order transitions.
- Order history must be append-only.
- Payment verification must happen before an order becomes `Paid`.
- Timeline displays should be based on recorded history.

## Guidance

- Treat order state changes as domain-significant operations.
- Keep history records immutable after creation.
- Do not hide failed or rejected payment attempts when they matter to auditing.
