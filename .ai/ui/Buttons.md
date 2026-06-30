# Buttons

## Variants

| Variant | Purpose |
| --- | --- |
| Primary | Main page action, submit, continue to payment |
| Secondary | Important neutral action |
| Outline | Alternative action with visible boundary |
| Ghost | Low-emphasis action in toolbars or rows |
| Danger | Destructive or irreversible action |
| Icon Button | Compact tool action with accessible label |
| Loading Button | Async submit or action in progress |
| Disabled | Unavailable action with clear disabled state |

## Anatomy

- Height: 40px default, 32px compact, 48px prominent.
- Radius: 8px.
- Horizontal padding: 16px default, 12px compact.
- Gap between icon and text: 8px.
- Font: Button token.

## Rules

- Each view should have one clear primary action.
- Destructive actions must not use primary styling.
- Loading buttons preserve width, disable repeated submit, and show progress.
- Disabled buttons must include context in nearby help or validation text when the reason is not obvious.
- Icon-only buttons require tooltip and accessible name.

## States

- Default, hover, active, focus, loading, disabled.
- Focus state must be visible and not rely only on color.
