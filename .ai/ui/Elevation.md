# Elevation

## Tokens

| Token | Shadow | Usage |
| --- | --- | --- |
| None | `none` | Most surfaces, tables, flat layouts |
| Low | `0 1px 2px rgba(15, 23, 42, 0.06)` | Cards and subtle raised controls |
| Medium | `0 8px 24px rgba(15, 23, 42, 0.10)` | Popovers, menus, drawers |
| High | `0 20px 48px rgba(15, 23, 42, 0.18)` | Dialogs and blocking overlays |

## Rules

- Use elevation sparingly; ServiceBridge should feel crisp, not floaty.
- Most application sections should be separated by border and spacing, not heavy shadow.
- Dialogs and drawers may use stronger elevation to clarify layering.
- Hover elevation must be subtle and paired with a stable layout.
