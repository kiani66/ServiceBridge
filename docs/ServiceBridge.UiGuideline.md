# ServiceBridge UI Guideline v1.1

## Document Authority

This is the source of truth for visual tokens and component styling: color, typography, spacing, radius, borders, shadows, buttons, forms, cards, DataGrid, status badges, focus, and responsive visual rules. Role flows, navigation, AppShell, realtime behavior, and page composition belong in the [UI/UX Blueprint](ServiceBridge.UIUX.Blueprint.md).

When documents conflict, use current code/tests first, then the Project Guideline for business and architecture, the UI/UX Blueprint for interaction, and this document for visual styling.

## Direction and Tokens

The visual direction is calm, precise, and operational: Stripe clarity, Vercel restraint, Linear density, and DigitalOcean practicality. Preserve English, left-aligned forms. Avoid decorative dashboard gradients, oversized marketing treatments, and nested cards.

| Token | Value / rule |
| --- | --- |
| `--sb-color-primary` | `#F97316`; primary actions and selected emphasis |
| `--sb-color-secondary` | `#2563EB`; supporting actions and information |
| `--sb-color-success` / `warning` / `danger` | `#16A34A` / `#F59E0B` / `#DC2626` |
| `--sb-color-bg` / `surface` | `#F6F8FB` / `#FFFFFF` |
| `--sb-color-text` / `muted` / `border` | `#111827` / `#5B6472` / `#E5E7EB` |
| Typography | system sans stack, with Inter preferred; compact left-aligned headings; no viewport-scaled type |
| Spacing | use a consistent 4px scale; prefer 8, 12, 16, 24, and 32px |
| Radius | 10px controls and standard cards; do not introduce competing shapes |
| Borders and shadows | subtle border first; shadows are small and functional |

## Components

- **Buttons:** one clear primary action per form; secondary actions are outlined/text; icon-only buttons have accessible labels and do not use wrapping labels.
- **Forms:** visible labels, concise `FormSection` grouping, field-local validation, and destructive actions separated from submit. Disabled actions state why they are unavailable.
- **Cards:** frame one entity, metric, or tool. Do not nest cards unless rendering a true repeated item.
- **DataGrid:** compact rows, concise headers, server-driven filtering/paging, status badges, and row actions at the right. Truncate low-priority single-line columns with a tooltip; allow meaningful notes to wrap only when it preserves scanning. Use horizontal scroll on narrow screens instead of compressing essential columns.
- **Status badges:** pair color with text and, where useful, an icon. Color alone never communicates status.
- **Loading:** use skeletons with stable dimensions for initial page and card loading; use unobtrusive inline progress for refreshes. Avoid layout jumps.

## Focus, Sensitive Values, and Responsive Rules

Keyboard focus must always be visible with a high-contrast, non-color-only outline/ring. Navigation focus styling must reflect actual focus and must not leave a misleading permanent outline around the initial selection.

Sensitive values—including credentials and inventory secrets—never render in a list row, preview, DOM/HTML, or normal grid before an authorized, intentional reveal action.

Sidebars collapse behind a clear navigation control on smaller screens. Grids become single-column where appropriate; tables/DataGrids scroll horizontally; actions stack only when necessary. Maintain readable English left alignment and adequate touch targets.
