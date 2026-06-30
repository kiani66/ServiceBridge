# Review Template

## Review Focus

Prioritize correctness, behavior regressions, security, architecture boundaries, and missing tests.

## Findings

List findings by severity with file and line references.

## Architecture Checks

- Layer boundaries remain intact.
- Commands mutate state.
- Queries return shaped DTOs.
- EF entities are not exposed directly.
- No unrelated broad changes.

## Validation Checks

Confirm build and test evidence when available.
