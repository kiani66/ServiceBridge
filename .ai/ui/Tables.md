# Tables

## Purpose

Tables are for operational views where admins or agents scan, compare, filter, and act on rows.

## Required Patterns

- Toolbar.
- Search.
- Filter.
- Sorting.
- Pagination.
- Bulk Actions.
- Row Actions.

## Rules

- Place search and primary filters in the toolbar.
- Keep table headers sticky when the table is tall and the layout supports it.
- Sorting state must be visible in the header.
- Pagination includes page size, current page, and total count when available.
- Bulk actions appear only after row selection.
- Row actions should be compact and predictable; destructive actions require confirmation.
- Sensitive credential data must be redacted or omitted unless the current role is explicitly authorized.

## Empty and Loading States

- Empty state explains the absence of data in one sentence and offers the next action when applicable.
- Loading state uses skeleton rows for table bodies.
- Error state includes retry when the operation is recoverable.
