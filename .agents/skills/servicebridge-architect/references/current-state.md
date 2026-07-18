# ServiceBridge Current-State Hint

> This file is only a navigation hint. The current repository, branch, git status, migrations, and tests are always the source of truth.

Last updated: 2026-07-15

## Known direction

- Product name: ServiceBridge; some code or historical references may still use PayBridge.
- Stack: ASP.NET Core 8, Web/API, EF Core, SQL Server, JWT + Cookie authentication, xUnit, layered monolith, CQRS-lite without MediatR.
- Core modules have progressed beyond the early guideline snapshot, including catalog, pricing, orders, payment, operational dashboards, tickets, notifications, audit-related flows, and SignalR-based realtime updates.
- Inventory and fulfillment work must preserve order and payment invariants.
- Repository has a codebase-memory-mcp index. Use it for discovery and trace paths, then verify actual files.

## Known architectural hotspot

`AdminOrderQueryRepository` is a navigation point for admin read concerns. When a task touches it, inspect its current size, responsibilities, and test coverage rather than assuming an earlier assessment still applies.

## Update rule

After major merges, update only stable facts here. Do not copy transient branch details, temporary test counts, or speculative roadmap items into the skill.
