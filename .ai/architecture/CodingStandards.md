# Coding Standards

## General

- Use ASP.NET Core 8 and EF Core 8 patterns already present in the repository.
- Keep changes small and reviewable.
- Preserve backward compatibility unless explicitly asked.
- Avoid broad refactors inside feature PRs.

## Application

- Commands mutate state.
- Queries return shaped DTOs.
- Use `ServiceResult` for expected business failures.
- Keep validation close to the use case or domain rule that owns it.

## Infrastructure

- Keep `DbContext` thin.
- Use `IEntityTypeConfiguration`.
- Use client-generated GUIDs.
- Configure IDs with `ValueGeneratedNever`.

## API and UI

- Do not return or bind EF entities directly.
- Keep controllers, endpoints, pages, and components thin.
