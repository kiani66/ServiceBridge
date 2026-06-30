# Feature Template

## Context

Read `.ai/PROJECT_CONTEXT.md` first. Summarize the relevant product and architecture context.

## Scope

Define the smallest reviewable feature slice.

## Out of Scope

State what must not change.

## Implementation Notes

- Commands mutate state.
- Queries return shaped DTOs.
- Do not return EF entities directly.
- Preserve backward compatibility unless explicitly asked.

## Validation

Run build and tests.
