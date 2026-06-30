# CQRS-lite

ServiceBridge uses CQRS-lite without MediatR.

## Commands

- Mutate state.
- Enforce use-case validation.
- Return `ServiceResult` for expected business failures.
- Capture immutable snapshots where required by the domain.

## Queries

- Do not mutate state.
- Return shaped DTOs.
- Avoid exposing persistence models directly.

## No MediatR

Do not introduce MediatR. Use explicit application services and handlers that fit the existing project style.
