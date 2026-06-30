# Folder Structure

## Solution Projects

- `ServiceBridge.Domain` - domain model and invariants.
- `ServiceBridge.Application` - use cases, commands, queries, DTOs, and `ServiceResult` outcomes.
- `ServiceBridge.Infrastructure` - EF Core, SQL Server, encryption, and integrations.
- `ServiceBridge.Api` - API host and transport concerns.
- `ServiceBridge.Web` - web UI, Blazor foundation, and temporary Razor Pages.
- `ServiceBridge.Tests` - automated tests.
- `ServiceBridge.Shared` - shared contracts or types where already established.

## AI Knowledge Base

- `.ai/PROJECT_CONTEXT.md` is the single source of truth.
- `.ai/domain/` contains domain-specific rules.
- `.ai/architecture/` contains implementation rules.
- `.ai/checklists/` contains review gates.
