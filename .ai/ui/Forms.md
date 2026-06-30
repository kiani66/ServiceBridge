# Forms

## Required Structure

Every form follows:

1. Label.
2. Input.
3. Help.
4. Validation.
5. Spacing.
6. Actions.

## Layout

- Single-column forms are the default.
- Two-column forms are allowed for admin metadata when fields are short and unrelated to credential entry.
- Credential and payment forms should stay single-column for clarity.
- Actions are aligned to the end of the form on desktop and full-width or stacked on mobile.

## Rules

- Group related fields under concise section headings.
- Required fields must be identified consistently.
- Submit buttons are disabled while submitting.
- Validation should appear as close as possible to the field it describes.
- Preserve entered values after validation errors whenever safe.
- Do not put business rules inside UI components; call shaped application/API contracts.

## Spacing

- Label to input: 8px.
- Input to help or validation: 8px.
- Field group gap: 16px.
- Section gap: 32px.
