# Dashboard

## Scope

This document defines dashboard design rules only. It does not implement dashboard pages.

## Patterns

- Top Summary Cards.
- Filters.
- Charts placeholder.
- Grid.
- Details Drawer.

## Rules

- Top summary cards show operational status, not decorative vanity metrics.
- Filters should be visible and close to the data they affect.
- Charts may be placeholders until real metrics are defined, but placeholder UI must not imply unsupported analytics.
- Dashboard grids should support scanning across payment, fulfillment, and assignment state.
- Details drawers provide quick inspection without losing list context.
- Empty, loading, and error states are required for every dashboard data region.

## Metrics Guidance

Prefer metrics that map to ServiceBridge operations:

- Orders waiting for payment.
- Paid orders awaiting assignment.
- Orders in fulfillment.
- Completed orders.
- Failed or rejected payments.
- Credential attention required.
