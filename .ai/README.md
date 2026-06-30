# ServiceBridge AI Operating System

This folder is the long-term AI operating system and project knowledge base for ServiceBridge.

Before implementing features, AI assistants and contributors must read `.ai/PROJECT_CONTEXT.md`. That file is the single source of truth for product context, architecture direction, business rules, roadmap sequencing, and working rules.

## Structure

- `PROJECT_CONTEXT.md` - single source of truth for AI assistants.
- `ROADMAP.md` - planned PR sequence and product direction.
- `AI_PLAYBOOK.md` - required AI working rules.
- `ARCHITECTURE.md` - system architecture and ownership boundaries.
- `BUSINESS_RULES.md` - core domain invariants.
- `UI_GUIDELINE.md` - Blazor UI guidance.
- `CHANGELOG.md` - documentation operating-system history.
- `architecture/` - layer, CQRS, naming, and folder rules.
- `domain/` - domain-specific notes for pricing, orders, payments, catalog, agents, and credentials.
- `ui/` - UI-specific working notes.
- `prompts/` - reusable PR, feature, bug fix, review, and migration prompts.
- `adr/` - architectural decision records.
- `checklists/` - PR, architecture, security, UI, and testing checklists.

## Scope

This folder documents how ServiceBridge should be understood and extended. It does not change application behavior, API contracts, database schema, or runtime configuration.
