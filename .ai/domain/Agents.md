# Agents

## Role

Agents process paid orders through assigned fulfillment paths.

## Rules

- Orders are assigned after payment is verified and the order becomes `Paid`.
- Agent-facing workflows must respect credential sensitivity.
- Agent actions should contribute to an auditable order timeline.

## Guidance

- Keep assignment and fulfillment rules explicit.
- Do not expose credentials outside authorized fulfillment contexts.
- Prefer clear status changes over implicit side effects.
