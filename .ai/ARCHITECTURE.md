# Architecture

ServiceBridge is an ASP.NET Core 8 layered monolith using EF Core 8 and SQL Server.

## Applications

- `ServiceBridge.Api` exposes API endpoints, authentication entry points, and integration-facing behavior.
- `ServiceBridge.Web` contains the web UI. The Blazor Web App foundation exists alongside temporary Razor Pages during migration.

## Style

ServiceBridge uses CQRS-lite without MediatR.

- Commands mutate state.
- Queries return shaped DTOs.
- Application services orchestrate use cases.
- Domain validates critical business transitions.
- Infrastructure handles EF Core, encryption, and external integrations.

## Authentication

ServiceBridge uses JWT and Cookie authentication where appropriate for API and web flows.

## Explicit Non-Goals

- No MediatR.
- No microservices.
- No workflow engines.
- No direct EF entity exposure to API or UI callers.

## Persistence Rules

- Keep `DbContext` thin.
- Use `IEntityTypeConfiguration`.
- Use client-generated GUIDs.
- Configure IDs with `ValueGeneratedNever`.
- Keep persistence concerns in Infrastructure.
