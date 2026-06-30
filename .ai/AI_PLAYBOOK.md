# AI Playbook

## Required First Step

Always read `.ai/PROJECT_CONTEXT.md` before implementing, reviewing, or planning ServiceBridge work.

## Working Rules

- Keep PRs small and reviewable.
- Never introduce broad unrelated changes.
- Always document Scope and Out of Scope.
- Always run `dotnet build ServiceBridge.sln`.
- Always run `dotnet test ServiceBridge.sln`.
- Preserve backward compatibility unless explicitly asked.
- Do not change runtime behavior for documentation-only work.
- Do not add migrations unless the requested change explicitly modifies schema.

## Implementation Discipline

- Follow the existing layered monolith boundaries.
- Keep API and Web layers thin.
- Use the Application layer for use cases.
- Use Domain for core invariants and state transitions.
- Use Infrastructure for EF Core, encryption, and external integrations.
- Do not return EF entities directly from API or UI surfaces.

## Required PR Notes

Every implementation PR should state:

- Scope.
- Out of Scope.
- Validation.
- Compatibility impact.
