# ServiceBridge Project Guideline

Source: ServiceBridge Project Guideline v1.3.

ServiceBridge is a production-oriented digital service mediation platform. It connects users in Iran to international digital services through local IRR pricing, secure payment, dynamic order data collection, controlled fulfillment, tracking, and operations.

The project was previously named PayBridge. PayBridge remains the MVP reference repository only. ServiceBridge is the production v1.0 repository and must use ServiceBridge naming in projects, namespaces, public branding, and documentation.

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

## PR 1 Scope

This PR creates only the repository foundation:

- Solution and project structure.
- Correct project references.
- BaseEntity.
- ServiceResult<T>.
- Shared constants.
- EF Core-ready Infrastructure shell.
- Initial docs and README.
- One passing xUnit test.

Out of scope for PR 1:

- Authentication.
- Catalog.
- Pricing.
- Orders.
- Payments.
- Admin panel.
- Agent panel.
- Database migrations.
- Business module implementation.
