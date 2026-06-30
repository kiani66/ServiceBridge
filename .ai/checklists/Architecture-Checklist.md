# Architecture Checklist

- Layered monolith boundaries are preserved.
- No MediatR introduced.
- No microservices introduced.
- No workflow engine introduced.
- Commands mutate state.
- Queries return shaped DTOs.
- EF entities are not returned directly to API or UI callers.
- `DbContext` remains thin.
- EF mappings use `IEntityTypeConfiguration`.
- IDs are client-generated GUIDs with `ValueGeneratedNever`.
