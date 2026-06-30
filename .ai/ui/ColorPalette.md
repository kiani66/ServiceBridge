# Color Palette

## Direction

ServiceBridge uses a modern SaaS palette inspired by Stripe, Vercel, Linear, and DigitalOcean. Colors should feel precise, trustworthy, and operational. Avoid heavy Bootstrap blues, legacy admin grays, and decorative gradients as primary UI language.

## Tokens

| Token | Hex | Usage |
| --- | --- | --- |
| Primary | `#2563EB` | Main actions, active navigation, selected states |
| Primary Hover | `#1D4ED8` | Hover state for primary controls |
| Primary Soft | `#EFF6FF` | Low-emphasis primary backgrounds and badges |
| Secondary | `#0F172A` | Secondary emphasis, strong neutral actions |
| Success | `#16A34A` | Successful payment, completed order, positive confirmation |
| Warning | `#D97706` | Pending review, attention required, non-blocking issues |
| Danger | `#DC2626` | Delete, failed payment, destructive action |
| Info | `#0891B2` | Informational notices and neutral status messaging |
| Background | `#F8FAFC` | Application background |
| Surface | `#FFFFFF` | Page sections, toolbars, forms |
| Card | `#FFFFFF` | Cards and contained component surfaces |
| Border | `#E2E8F0` | Default borders and dividers |
| Text Primary | `#0F172A` | Main text |
| Text Secondary | `#475569` | Secondary copy and metadata |
| Muted | `#94A3B8` | Placeholders, tertiary labels, inactive icons |
| Disabled | `#CBD5E1` | Disabled border, icon, and text |

## State Rules

- Primary actions use `Primary` with white text.
- Hover states must visibly change color, border, or background.
- Disabled states must reduce contrast and remove pointer affordance.
- Status colors must be paired with text or icons; never rely on color alone.
- Payment and credential warnings must use restrained status color, not decorative banners.

## Contrast

- Body text must meet WCAG AA contrast against its background.
- Interactive text and icon buttons must remain legible in hover, active, focus, and disabled states.
- `Primary Soft` should be paired with `Primary Hover` or `Text Primary`, not low-contrast blue text.
