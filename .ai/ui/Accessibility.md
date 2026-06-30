# Accessibility

## Requirements

- Keyboard navigation.
- Visible focus.
- Color contrast.
- Screen reader friendly markup.

## Rules

- Every interactive element must be reachable and operable by keyboard.
- Focus states must be visible on all controls.
- Icon-only controls require accessible labels.
- Form validation must be programmatically associated with the related input.
- Dialogs and drawers must manage focus correctly.
- Do not rely on color alone to communicate status.
- Tables must use proper headers and labels.
- Loading states should announce progress when the wait affects user action.
- Error messages should identify what failed and what the user can do next.

## Sensitive Data

- Credential fields must make masking and reveal behavior explicit.
- Revealing sensitive values requires intentional user action and authorization.
- Screen reader labels must not expose secrets in broad contexts.
