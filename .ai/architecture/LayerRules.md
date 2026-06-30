# Layer Rules

## Domain

- Owns entities, value rules, and domain invariants.
- Validates order transitions.
- Does not depend on Application, Infrastructure, API, or Web.

## Application

- Owns use cases, commands, queries, DTOs, and orchestration.
- Uses `ServiceResult` for expected business failures.
- Does not expose EF entities as API or UI contracts.

## Infrastructure

- Owns EF Core, SQL Server persistence, encryption, and external integrations.
- Implements repository or persistence details required by Application.
- Uses `IEntityTypeConfiguration` for entity mapping.

## API and Web

- Stay thin.
- Authenticate, validate transport concerns, call Application, and shape responses.
- Do not contain core business rules.
