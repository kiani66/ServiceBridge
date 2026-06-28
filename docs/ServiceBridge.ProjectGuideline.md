# ServiceBridge Project Guideline

Source: ServiceBridge Project Guideline v1.3.

ServiceBridge is a production-oriented digital service mediation platform. It connects users in Iran to international digital services through local IRR pricing, secure payment, dynamic order data collection, controlled fulfillment, tracking, and operations.

The production repository must use ServiceBridge naming in projects, namespaces, public branding, and documentation. The prior MVP repository is a reference source only and must not be modified during ServiceBridge cleanup work.

## Product Roles

- User: browses services, selects variants, submits required information, pays, and tracks orders.
- Agent: manually fulfills assigned orders, requests more information, adds operational notes, and completes or fails work.
- Admin: manages users, agents, catalog, pricing, orders, payments, tickets, reports, settings, and audit-sensitive operations.

## Core Flow

```text
1. User browses the catalog.
2. User selects a service variant.
3. System calculates the IRR price.
4. User submits required fields or credentials.
5. User pays.
6. Payment is verified.
7. Order becomes Paid.
8. Admin assigns the order to an Agent, or inventory/automation fulfills it.
9. Agent processes manual fulfillment.
10. Agent completes fulfillment.
11. User sees the final result and timeline.
```

Pricing formula:

```text
FinalPriceIRR = BaseForeignPrice * ExchangeRate * (1 + MarginPercent / 100)
```

Order prices must be snapshotted at order creation time. Later exchange-rate or margin changes must never change existing orders.

## Migration Baseline

The current ServiceBridge codebase preserves the migrated MVP behavior and test coverage while using ServiceBridge naming. Cleanup PRs must be limited to migration safety, configuration hygiene, documentation, and validation unless a later roadmap PR explicitly authorizes business feature work.

Allowed migration cleanup:

- Rename-related fixes.
- Documentation of migration risks.
- Build and test validation.
- Low-cost tests that lock architecture or migration invariants.

Out of scope for migration cleanup:

- Authentication.
- Catalog V2 or ServiceVariant.
- Payment hardening.
- Tickets, notifications, inventory, wallet, or audit improvements.
- Database migrations.
- Business behavior changes.
