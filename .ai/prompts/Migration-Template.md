# Migration Template

## Schema Intent

Describe the required schema change and why it is needed.

## Compatibility

Describe backward compatibility, data migration, and rollback considerations.

## EF Rules

- Use `IEntityTypeConfiguration`.
- Keep `DbContext` thin.
- Use client-generated GUIDs.
- Use `ValueGeneratedNever` for IDs.

## Validation

- Inspect generated migration.
- Run `dotnet build ServiceBridge.sln`.
- Run `dotnet test ServiceBridge.sln`.

## Out of Scope

Do not mix unrelated behavior or UI changes into a migration PR.
