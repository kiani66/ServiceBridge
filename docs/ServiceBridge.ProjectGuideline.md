# ServiceBridge Project Guideline v1.4

## Document Authority

This is the authoritative document for ServiceBridge product definition, business and domain rules, architecture, security, current implementation state, roadmap, testing strategy, and Definition of Done. Interaction and page behavior belong in the [UI/UX Blueprint](ServiceBridge.UIUX.Blueprint.md); visual tokens and component styling belong in the [UI Guideline](ServiceBridge.UiGuideline.md).

When documents conflict, use: (1) current code and tests, (2) this guideline for business and architecture, (3) the UI/UX Blueprint for interaction and page behavior, and (4) the UI Guideline for visual styling.

**Last verified:** 2026-07-11

**Repository baseline:** `main` / `e732b15` — inventory foundation merge.
**Test status:** no latest repository-verified passing total was available without running the suite, so this document deliberately does not state a test count.

## Product Definition

ServiceBridge is a digital-service mediation platform for customers in Iran. It provides catalog discovery, IRR pricing, secure order-data collection, payment, controlled fulfillment, order tracking, customer support, and operational administration.

The solution is `ServiceBridge.sln` with these projects:

- `ServiceBridge.Domain` — entities, value objects, enums, and domain-owned transitions.
- `ServiceBridge.Application` — commands, queries, DTOs, interfaces, and expected-failure results.
- `ServiceBridge.Infrastructure` — EF Core persistence, configurations, encryption, and implementation services.
- `ServiceBridge.Api` — authenticated HTTP API, SignalR hub, and composition root.
- `ServiceBridge.Web` — the Blazor web application and its shared AppShell.
- `ServiceBridge.Shared` — cross-layer constants and messages.
- `ServiceBridge.Tests` — focused domain, data, service, and UI-related tests.

ServiceBridge evolved from an earlier internal MVP and repository structure.

## Product Roles and Core Flows

- **User:** browses services and variants, supplies required data, pays, follows the order timeline, responds to requests, receives notifications, and opens or replies to support tickets.
- **Agent:** works assigned orders, handles customer data and operational notes under authorization, requests information, and performs permitted status transitions.
- **Admin:** operates catalog, pricing, users, orders, assignment, tickets, refunds, reports, audit views, notifications, and other privileged operations.

Core order flow: catalog and variant selection → price calculation and immutable snapshot → required-data submission → payment → verified paid order → assignment or eligible fulfillment → processing → customer-visible timeline and final result.

`FinalPriceIRR = BaseForeignPrice × ExchangeRate × (1 + MarginPercent / 100)`.

Order price snapshots are immutable: later changes to rate, margin, catalog, or variant data must not rewrite an existing order.

## Current Implementation Status

### Completed and merged

- Authentication and role authorization for User, Agent, and Admin.
- Structured catalog, providers, service variants, pricing, and immutable price snapshots.
- Public catalog, service detail, checkout, user orders, order detail, payment foundation, and payment-result pages.
- Admin order assignment, agent processing workspace, customer-information/action flow, and order transparency timeline.
- In-app notifications, outbox-related delivery foundations, and authenticated realtime update foundation.
- Audit infrastructure, audit logs, operational audit views, financial reports, refund foundation, and admin operations pages.
- Ticket and `TicketMessage` support, including customer and admin flows.
- Inventory foundation: `InventoryItem`, encrypted-at-rest code protection, configuration/migration, and focused domain/data/protection tests.
- Blazor `ServiceBridge.Web` AppShell, role navigation, design system, and the currently implemented `/ui/*` experience.

### In progress but not merged

The working tree contains uncommitted inventory-management source and tests on `codex/admin-inventory-management`. They are not part of this baseline and must not be represented as merged product capability.

### Not started or remaining MVP work

- Inventory reservation, release, and idempotent delivery integration with paid orders.
- Operational reports and analytics beyond the implemented financial reports.
- Broader integration-test hardening and deployment-readiness verification.

### Explicitly out of scope for v1.0

- Microservices, a workflow engine, MediatR, and a generic warehouse or physical-stock domain.
- Ticket live chat, attachments, SLA automation, category routing, AI support, and advanced assignment unless code explicitly introduces them.
- Unbounded automation that bypasses domain transitions, payment eligibility, authorization, or audit requirements.

## Ticket Boundaries

Tickets and `TicketMessage` are supported. Ticket messages are append-only. User messages cannot be internal; repository queries exclude internal messages from user-facing results. A closed ticket rejects new replies. Public user DTOs must not expose internal assignment or category fields.

This is support-ticket messaging, not live chat. Do not claim attachments, SLA automation, category routing, AI assistance, or advanced assignment without a merged implementation.

## Realtime Updates

SignalR is a delivery mechanism, not a business-rule layer. `ServiceBridge.Application` depends on `IRealtimeEventPublisher`; the API implements it through `SignalRRealtimeEventPublisher` and hosts the authenticated `RealtimeHub` at `/realtimeHub`.

The server, never the client, determines safe groups: `user:{userId}`, `agent:{agentId}`, `admins`, and `order:{orderId}`. Clients must not select arbitrary authorization groups. The current hub adds user, agent, and admin groups from authenticated claims; order targeting is published server-side.

`AppShell` owns one scoped `RealtimeClient` connection. Pages and components subscribe only while active and unsubscribe on disposal. Reconnection and duplicate subscriptions must be safe. Events refresh the affected authorized query data; they do not mutate business state on the client or replace API authorization/database truth. The implemented event family covers notifications, order updates and assignment, customer-action changes, and ticket updates. Online authorized users should receive targeted refreshes for relevant notification counts, timelines, assignment/customer-action state, tickets, and counters.

## Inventory Rules

Inventory is only for `InventoryCode` fulfillment. Codes and secrets are encrypted at rest and plain values must never appear in list DTOs, logs, audit snapshots, or normal admin grids.

The next inventory work must preserve these invariants:

- Reservation is transactional; one item cannot be reserved for two orders.
- Only eligible Paid orders can reserve inventory.
- Delivery is idempotent.
- Reservation, release, reveal, and delivery actions are auditable.
- Do not invent a generic warehouse or physical-stock model for the MVP.

Entity shape beyond the merged foundation remains a design decision; document and implement behavior/invariants before expanding schema.

## Architecture and Security Rules

- Use a layered monolith: Domain, Application, Infrastructure, API, and Web.
- Do not add MediatR, microservices, or a workflow engine.
- Commands mutate; queries return purpose-shaped DTOs. Never expose EF entities from API or UI boundaries.
- Keep `DbContext` thin. Use `IEntityTypeConfiguration<T>`, avoid unnecessary `Include` chains, and use server-shaped projections.
- IDs are client-generated `Guid` values configured with `ValueGeneratedNever()`.
- Return `ServiceResult<T>` for expected business failures; reserve exceptions for exceptional conditions.
- Domain-owned transitions protect order, ticket, payment, and inventory state rules.
- Pricing snapshots are immutable; credential encryption belongs to Infrastructure.
- Order history, ticket messages, payment events, and audit logs are append-only.
- Write audits explicitly at business actions. Do not add a generic automatic EF audit interceptor.
- Sensitive credentials and inventory secrets require explicit, authorized reveal behavior and must not be rendered or logged by default.

## Critical Path

1. Complete inventory reservation and paid-order delivery flow.
2. Add operational reporting and analytics needed to operate that flow.
3. Harden integration tests around payment, authorization, realtime refresh, inventory concurrency, and irreversible transitions.
4. Complete deployment-readiness checks and production operational verification.

This is capability-based. Historical pull-request numbers are evidence, not the roadmap.

## Testing Strategy and Definition of Done

Tests should lock domain invariants, expected business failures, data mappings, authorization boundaries, event publication behavior, and role-specific UI/navigation expectations. Add focused tests alongside changed behavior; use integration coverage for cross-layer flows and concurrency-sensitive reservation/delivery behavior.

A change is done when it has a clear owner and authorization boundary, preserves domain invariants and append-only records, uses shaped DTOs, handles loading/error/empty/forbidden UI states where applicable, has proportionate tests, is validated with the required repository checks for its scope, contains no secrets, and updates the governing documentation when product behavior changes.
