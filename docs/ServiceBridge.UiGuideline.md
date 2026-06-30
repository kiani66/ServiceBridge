# ServiceBridge UI Guideline

## Design inspiration

ServiceBridge uses a modern SaaS interface direction:

- Stripe: clear checkout and service detail flows.
- Vercel: restrained surfaces, high contrast text and simple navigation.
- Linear: operational density, consistent spacing and fast scanning.
- DigitalOcean: practical admin/service management screens.

The interface should feel calm, precise and work-focused. Avoid decorative
hero treatments, heavy gradients, oversized marketing sections and nested card
stacks on dashboard surfaces.

## Color palette

- Primary: `#2563EB`
- Accent: `#38BDF8`
- Success: `#16A34A`
- Warning: `#D97706`
- Danger: `#DC2626`
- Background: `#F8FAFC`
- Surface: `#FFFFFF`
- Text: `#0F172A`
- Muted text: `#64748B`
- Border: `#E2E8F0`

Use white cards on a light neutral background. Use soft blue for primary
actions, selected states and navigation accents. Status colors should be muted
and semantic, not saturated.

## Typography direction

- Use a system sans-serif stack with Inter as the preferred first choice.
- Keep headings compact and left-aligned.
- Do not use viewport-scaled typography.
- Use regular letter spacing.
- Reserve large display text for public landing or catalog introduction areas.

## Layout rules

- Use an app shell with a top bar and optional sidebar for dashboard work.
- Keep content width constrained for forms and details, wider for data grids.
- Use 12px rounded corners for cards and 10px for controls.
- Use subtle borders first; shadows should be minimal.
- Avoid putting cards inside cards.
- Prefer full-width bands or unframed sections for page structure.

## Form rules

- Forms are left-aligned and English.
- Group fields into `FormSection` blocks with short section labels.
- Keep labels visible and explicit.
- Use one primary submit action per form.
- Put destructive actions away from the primary action.
- Validation messages should be close to the field they explain.

## Button rules

- Primary actions use soft blue filled buttons.
- Secondary actions use outlined or text buttons.
- Icon buttons should use familiar Material/MudBlazor icons and accessible
  labels.
- Button text should be direct: `Save`, `Create variant`, `Assign agent`.
- Avoid long button labels that wrap unpredictably.

## Card rules

- Cards represent a single entity, metric or framed tool.
- Cards use white backgrounds, subtle borders and small shadows.
- Avoid decorative gradients in operational UI.
- Keep card headings compact.
- Do not nest cards unless the inner content is an actual repeated item.

## Table and DataGrid rules

- Tables should prioritize scanning and comparison.
- Use compact row height for admin and agent workflows.
- Use muted uppercase headers.
- Keep status in badges.
- Put row actions at the far right.
- Prefer server-shaped DTOs; do not bind EF entities directly to UI.

## Dashboard rules

- Dashboards should show the next operational action quickly.
- Sidebar navigation should stay predictable across Admin and Agent areas.
- Top bars should contain page context, search or account actions only.
- Use metrics sparingly and make them actionable.
- Avoid marketing copy in admin or agent dashboards.

## Public catalog direction

- Public catalog pages should feel polished and trustworthy.
- Service and variant cards must show actual purchasable details:
  price, currency, region, delivery type and fulfillment expectations.
- Checkout-style interactions should be direct and readable.
- Keep existing `/plans` compatibility until the cleanup PR removes it.

## User dashboard direction

- User dashboards should focus on orders, payment status and required actions.
- Use clear status badges and short next-step text.
- Do not expose internal agent/admin terminology.
- Order details should preserve immutable price snapshots.

## Agent dashboard direction

- Agent dashboards should be dense, operational and fast.
- Prioritize assigned orders, credential handling, notes and status actions.
- Sensitive data must have explicit reveal/view auditing flows.
- Keep forms compact and avoid decorative cards.

## Admin dashboard direction

- Admin screens should support repeated service, catalog and user management.
- Use tables, filters, segmented controls and precise forms.
- Keep catalog pricing and variant management visually consistent.
- Avoid redesigning business flows inside UI-only foundation PRs.

## Accessibility basics

- Maintain visible focus states.
- Use semantic headings in order.
- Ensure all icon-only actions have accessible labels.
- Keep color contrast high enough for text and status badges.
- Do not rely on color alone to communicate status.

## Mobile responsiveness

- Sidebars collapse behind navigation controls.
- Grids collapse to one column below tablet widths.
- Buttons wrap into vertical stacks only when needed.
- Tables should scroll horizontally rather than compressing text into unreadable
  columns.
- Forms must remain left-aligned and readable on small screens.

