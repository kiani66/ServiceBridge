# ServiceBridge Architecture

Source: ServiceBridge Project Guideline v1.3.

ServiceBridge must remain a clean, testable, production-ready monolith using Clean/Onion architecture.

## Layers

```text
ServiceBridge.Web
ServiceBridge.Api
        |
ServiceBridge.Application
        |
ServiceBridge.Domain
        ^
ServiceBridge.Infrastructure
        |
SQL Server
```

## Project Responsibilities

- ServiceBridge.Domain: entities, enums, value objects, state machines, and business rules. It must not depend on EF Core, API, Web, or Infrastructure.
- ServiceBridge.Application: commands, queries, DTOs, interfaces, orchestration, and ServiceResult<T>. Expected business failures should use result types, not generic exceptions.
- ServiceBridge.Infrastructure: EF Core, repository implementations, encryption, password hashing, JWT generation, data protection, payment gateway integration, and external services.
- ServiceBridge.Api: controllers, authorization, middleware, HTTP contracts, API configuration, and later Swagger configuration.
- ServiceBridge.Web: Razor Pages, UI forms, cookie authentication, dashboards, and role-based pages.
- ServiceBridge.Shared: constants, shared messages, and lightweight shared models.

## Non-Negotiable Rules

- Keep the monolith.
- Do not introduce MediatR.
- Do not introduce microservices.
- Do not introduce workflow engines.
- Keep commands and queries separate.
- Commands mutate state.
- Queries return shaped DTOs.
- Do not return EF entities directly to API or UI.
- Keep DbContext thin.
- Use IEntityTypeConfiguration<T> for entity configuration.
- Use client-generated GUID IDs and ValueGeneratedNever() in mappings.
- Keep pricing snapshots immutable.
- Store service credentials encrypted.
- Never log decrypted credentials.
- Keep order history append-only.
- Validate order transitions in Domain.
- Admin and Agent actions must be auditable.
- Verify payment callbacks before confirming payment.

## PR 1 Architecture Status

PR 1 creates the architectural shell only. Infrastructure is EF Core-ready, but it has no DbSet, no entity mappings, and no migrations until business entities are introduced in later PRs.
