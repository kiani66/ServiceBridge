# ADR-0001: Layered Monolith

## Status

Accepted.

## Decision

ServiceBridge is a layered monolith.

## Context

ServiceBridge needs strong domain consistency across catalog, pricing, orders, payments, credentials, and fulfillment. The current scale and product stage favor clear in-process boundaries over distributed services.

## Consequences

- Domain, Application, Infrastructure, API, and Web responsibilities stay separate.
- No microservices.
- No workflow engines.
- Integration complexity remains contained in Infrastructure.
