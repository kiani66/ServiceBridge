# Component Catalog

## Purpose

The component catalog defines reusable UI components and the rules every future implementation should follow. This is a documentation catalog, not a page implementation.

## Button

- Purpose: Trigger an action or submit a workflow step.
- Props: `variant`, `size`, `icon`, `disabled`, `loading`, `ariaLabel`.
- Usage: Primary page actions, secondary actions, row actions, toolbar commands.
- States: Default, hover, active, focus, loading, disabled.
- Example: Primary submit button for continuing from order details to payment.

## Icon Button

- Purpose: Compact action for toolbars, rows, or navigation.
- Props: `icon`, `ariaLabel`, `tooltip`, `size`, `disabled`.
- Usage: Search clear, row menu, close drawer, refresh.
- States: Default, hover, active, focus, disabled.
- Example: Close button in an order detail drawer.

## Text Field

- Purpose: Capture short text values.
- Props: `label`, `value`, `placeholder`, `helpText`, `validationMessage`, `required`, `disabled`, `readOnly`.
- Usage: Order fields, admin metadata, search forms.
- States: Default, focus, filled, invalid, disabled, read-only.
- Example: Required account identifier field for a service order.

## Password Field

- Purpose: Capture sensitive credentials.
- Props: `label`, `value`, `helpText`, `validationMessage`, `required`, `revealAllowed`.
- Usage: Credential collection during order submission.
- States: Masked, reveal requested, focus, invalid, disabled.
- Example: Password credential for fulfillment where the service requires account access.

## Select

- Purpose: Pick one option from a known list.
- Props: `label`, `options`, `value`, `placeholder`, `helpText`, `validationMessage`, `disabled`.
- Usage: Service variant, status, assignment, filters.
- States: Default, open, selected, invalid, disabled.
- Example: Variant selector that drives pricing context.

## Autocomplete

- Purpose: Search and select from a larger remote or local set.
- Props: `label`, `query`, `options`, `selected`, `loading`, `emptyMessage`, `validationMessage`.
- Usage: Agent assignment, service search, admin catalog lookup.
- States: Empty, typing, loading, results, no results, invalid.
- Example: Admin assigns a paid order to an agent.

## Checkbox

- Purpose: Toggle one or more independent boolean options.
- Props: `label`, `checked`, `disabled`, `helpText`.
- Usage: Bulk selection, optional acknowledgements, filter options.
- States: Unchecked, checked, indeterminate, focus, disabled.
- Example: Select rows for bulk admin action.

## Radio Group

- Purpose: Choose one option from a small exclusive set.
- Props: `label`, `options`, `value`, `helpText`, `validationMessage`, `disabled`.
- Usage: Payment method choice or fulfillment path choice when options are few.
- States: Unselected, selected, focus, invalid, disabled.
- Example: Choose a fulfillment route for an order.

## Switch

- Purpose: Toggle an immediate binary setting.
- Props: `label`, `checked`, `disabled`, `helpText`.
- Usage: Enable/disable catalog visibility or admin settings.
- States: Off, on, focus, disabled.
- Example: Mark a service variant as visible in the public catalog.

## Search Field

- Purpose: Filter lists and operational data.
- Props: `value`, `placeholder`, `onClear`, `loading`, `disabled`.
- Usage: Catalog search, order search, agent work queue search.
- States: Empty, filled, loading, focus, disabled.
- Example: Search orders by order number or user.

## Card

- Purpose: Present a discrete summary or repeated item.
- Props: `title`, `subtitle`, `status`, `actions`, `interactive`.
- Usage: Stat card, price card, order card, service card, information card.
- States: Default, hover, active, selected, loading.
- Example: Service card showing variants and fulfillment summary.

## Table

- Purpose: Show operational rows for comparison and action.
- Props: `columns`, `rows`, `sort`, `pagination`, `selection`, `loading`, `emptyState`.
- Usage: Admin orders, payments, agents, catalog management.
- States: Loading, empty, sorted, selected rows, error.
- Example: Admin order list with payment and fulfillment state.

## Pagination

- Purpose: Navigate paged data.
- Props: `page`, `pageSize`, `totalCount`, `disabled`.
- Usage: Tables and large result sets.
- States: First page, middle page, last page, loading, disabled.
- Example: Admin catalog page list.

## Badge

- Purpose: Show compact status or classification.
- Props: `tone`, `label`, `icon`.
- Usage: Payment state, order state, credential status, role.
- States: Neutral, info, success, warning, danger.
- Example: `Paid`, `Awaiting assignment`, `Credential required`.

## Dialog

- Purpose: Block for confirmation or focused edit.
- Props: `title`, `description`, `open`, `actions`, `size`.
- Usage: Confirmation, delete, edit, information.
- States: Open, loading, validation error, submission error.
- Example: Confirm deleting an unpublished service variant.

## Drawer

- Purpose: Show contextual detail without leaving a list.
- Props: `title`, `open`, `position`, `size`, `actions`.
- Usage: Order detail, timeline, filters, payment detail.
- States: Open, loading, error, dirty, saving.
- Example: Order detail drawer from dashboard grid.

## Breadcrumb

- Purpose: Orient users in nested app areas.
- Props: `items`, `current`.
- Usage: Admin, agent, and deep user pages.
- States: Current item, navigable parent, collapsed.
- Example: Admin / Catalog / Service / Variant.

## Sidebar

- Purpose: Role-specific primary navigation.
- Props: `items`, `activeItem`, `collapsed`, `role`.
- Usage: User, agent, and admin shells.
- States: Expanded, collapsed, active, disabled item.
- Example: Admin sidebar with orders, payments, catalog, agents.

## Topbar

- Purpose: Global app context and account actions.
- Props: `title`, `actions`, `userMenu`, `search`.
- Usage: Authenticated app shell.
- States: Default, menu open, search active.
- Example: Admin topbar with account menu and global search.

## Empty State

- Purpose: Explain missing data and offer next action.
- Props: `title`, `description`, `icon`, `action`.
- Usage: Tables, dashboards, cards, search results.
- States: No data, no search results, filtered out.
- Example: No paid orders awaiting assignment.

## Skeleton

- Purpose: Preserve layout while content loads.
- Props: `shape`, `lines`, `animated`.
- Usage: Cards, tables, forms, dashboards.
- States: Loading, reduced motion.
- Example: Skeleton rows while the order table loads.
