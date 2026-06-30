# Animations

## Direction

Animations should be subtle and functional. They should help users understand state changes without adding visual noise.

## Allowed Patterns

- Fade.
- Slide.
- Skeleton.
- Loading.

## Rules

- Keep durations between 120ms and 240ms for most interactions.
- Use easing that feels responsive, not bouncy.
- Respect reduced-motion preferences.
- Loading indicators should preserve layout dimensions.
- Skeletons should approximate final content shape.
- Avoid flashy animations, decorative motion, and looping effects that distract from operational tasks.

## Usage

- Fade for menus, popovers, and status messages.
- Slide for drawers and mobile navigation.
- Skeleton for tables, cards, dashboards, and detail panels.
- Loading spinners only for compact controls or short operations.
