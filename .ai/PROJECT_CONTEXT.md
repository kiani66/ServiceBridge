# ServiceBridge Project Context

This file is the single source of truth for AI assistants working on ServiceBridge. Always read it before implementing features, reviewing code, creating plans, or proposing architecture changes.

## Product Identity

ServiceBridge is a digital service mediation and payment platform, not an e-commerce store.

The platform helps users request digital services, submit the fields or credentials required for fulfillment, pay in IRR, and track the fulfillment timeline while admins and agents process the order.

## Main Business Flow

1. User selects a service and variant.
2. System calculates the IRR price.
3. User submits order fields and, when required, credentials.
4. User pays.
5. Payment is verified.
6. Order becomes `Paid`.
7. Admin assigns the order to an Agent or fulfillment path.
8. Agent processes the order.
9. Order completes.
10. User tracks the timeline.

## Pricing

The canonical pricing formula is:

```text
ForeignPrice x ExchangeRate x (1 + MarginPercent / 100) = FinalPriceIRR
```

Pricing snapshots are immutable. Once an order captures a pricing snapshot, later pricing or exchange-rate changes must not rewrite the order's historical price.

## Business Invariants

- Credentials must be encrypted at rest.
- Order history must be append-only.
- Payment callback data must be verified before confirming payment.
- Domain code validates order transitions.
- Commands mutate state.
- Queries return shaped DTOs.
- EF entities must not be returned directly to API or UI callers.

## Architecture

ServiceBridge uses:

- ASP.NET Core 8.
- EF Core 8.
- SQL Server.
- Layered monolith architecture.
- `ServiceBridge.Api` and `ServiceBridge.Web`.
- Blazor Web App foundation alongside temporary Razor Pages.
- CQRS-lite without MediatR.
- JWT and Cookie authentication.

ServiceBridge does not use:

- MediatR.
- Microservices.
- Workflow engines.

## Layer Ownership

- Domain owns entities, value rules, and order transition validation.
- Application owns use cases, commands, queries, DTO shaping, and expected business failures through `ServiceResult`.
- Infrastructure owns EF Core, encryption, external integrations, persistence configuration, and integration adapters.
- API and Web layers stay thin and call application services.

## Data Rules

- Keep `DbContext` thin.
- Use `IEntityTypeConfiguration`.
- Use client-generated GUIDs.
- Use `ValueGeneratedNever` for IDs.
- Do not add migrations unless the requested change explicitly changes schema.

## Roadmap Direction

After PR #9A:

- PR #9B: UI Design System.
- PR #10: UI Prototype with mock data.
- PR #11: Public Catalog UX on Blazor.
- PR #12: Checkout / Dynamic Order Form.
- PR #13: User Order Tracking.
- PR #14: Payment Hardening / UX.
- PR #15: Agent Dashboard.
- PR #16: Admin Operations + Audit.

## AI Working Rules

- Always read this file before implementing features.
- Keep PRs small and reviewable.
- Never introduce broad unrelated changes.
- Always document Scope and Out of Scope.
- Always run build and tests.
- Preserve backward compatibility unless explicitly asked.
- For UI work, follow `.ai/UI_GUIDELINE.md` and `.ai/ui/README.md`.
- For domain work, follow `.ai/BUSINESS_RULES.md` and files under `.ai/domain/`.
