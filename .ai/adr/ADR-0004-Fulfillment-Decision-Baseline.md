# ADR-0004: Fulfillment Decision Baseline

## Status

Accepted.

## Decision

ServiceBridge remains a layered modular monolith. MediatR, microservices,
workflow engines, and speculative plugin frameworks are not approved.

Strong domain modeling is incremental and feature-owned. A project-wide
migration of primitives, strings, GUIDs, or identifiers to new value objects or
strongly typed IDs is prohibited.

The fulfillment model uses three independent decision axes:

- `ProcurementStrategy` describes how the required capability is sourced.
- `FulfillmentMode` describes how a confirmed order is executed.
- `DeliveryArtifactType` describes what result is delivered to the customer.

Provider names are integration details and must not control domain branching.

Payment confirmation records the payment outcome but must not directly execute
Agent, Inventory, or Supplier fulfillment. New fulfillment methods should
primarily require a focused handler, dependency-injection registration, and
focused tests without rewriting Order or Payment.

This ADR establishes the decision baseline only. Generalized runtime
Fulfillment implementation is deferred to later PRs.

## Migration sequence

Runtime migration occurs only after the Policy, Order Snapshot, and Fulfillment
Execution foundations exist, in this dependency order:

```text
Architecture Guardrails
→ Fulfillment Policy Foundation
→ Order Fulfillment Snapshot
→ Fulfillment Execution Foundation
→ Fulfillment Coordinator and existing flow migration
```

Each stage remains separate PR scope; this ADR introduces no Inventory,
Procurement, or Fulfillment runtime implementation.

## Current-state note

The repository currently has an Inventory Code path in which payment
confirmation invokes the existing inventory fulfillment command. That merged
behavior is not changed by this ADR and is the first known path to move behind
the approved fulfillment boundary in a later, runtime-focused PR.

## Consequences

- Fulfillment capabilities evolve inside the existing layer and module
  boundaries.
- Domain types are introduced only where a feature owns and benefits from them.
- Procurement choice, fulfillment execution, and delivery representation can
  evolve independently.
- Provider-specific adapters translate to domain concepts before domain
  decisions are made.
- This PR changes no production orchestration, state transition, persistence,
  contract, or runtime behavior.
