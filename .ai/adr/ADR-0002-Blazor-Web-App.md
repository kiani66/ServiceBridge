# ADR-0002: Blazor Web App

## Status

Accepted.

## Decision

ServiceBridge has a Blazor Web App foundation in `ServiceBridge.Web`.

## Context

The platform needs a richer UI for catalog browsing, dynamic order forms, payment status, user tracking, agent work, and admin operations.

## Consequences

- Blazor exists alongside temporary Razor Pages during migration.
- New UI work should follow the Blazor foundation unless the task explicitly targets Razor Pages.
- Business logic remains outside UI components.
