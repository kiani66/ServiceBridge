# ServiceBridge UI/UX Blueprint v1.1

## Document Authority

This is the authoritative experience contract for navigation, AppShell, page composition, role interactions, realtime UX, and data-driven page states in the current `ServiceBridge.Web` Blazor application. Product/architecture rules belong in the [Project Guideline](ServiceBridge.ProjectGuideline.md); visual tokens and component styling belong in the [UI Guideline](ServiceBridge.UiGuideline.md).

When documents conflict, use current code/tests first, then the Project Guideline for business and architecture, this blueprint for interaction/page behavior, and the UI Guideline for visual styling.

## Purpose and Technology Direction

This is not a pre-Blazor proposal. It governs the implemented Blazor experience: role-aware pages inside the shared `AppShell`, backed by the authenticated API and purpose-shaped DTOs. Pages must preserve server authorization and domain rules; the Web application presents and refreshes authorized state, rather than owning business state.

## AppShell and Role Navigation

User, Agent, and Admin use one design system and one visual identity. Roles change navigation and available actions, never the shell’s typography, spacing, top bar, sidebar, account menu, logout affordance, or responsive breakpoints. Page-level components do not create competing shells.

Every authenticated shell provides obvious account/profile and logout actions. Navigation focus is visible only when focused; it must not permanently outline the initially selected item.

### Current routes

| Role | Implemented routes |
| --- | --- |
| Public | `/ui`, `/ui/catalog`, `/ui/catalog/{CategorySlug}`, `/ui/services/{ServiceSlug}`, `/ui/checkout`, payment result routes |
| User | `/ui/dashboard`, `/ui/orders`, `/ui/orders/{OrderId}`, `/ui/tickets`, `/ui/tickets/new`, `/ui/tickets/{TicketId}`, `/ui/notifications`, profile routes |
| Agent | `/ui/agent`, `/ui/agent/orders`, `/ui/agent/orders/{OrderId}` |
| Admin | `/ui/admin`, catalog/pricing/users/orders routes, tickets, refunds, financial reports, audit, audit logs |

Inventory-management UI is planned: it is not an implemented route in this baseline.

## Page Composition and Operational Interaction

Use a clear page header, primary action where permitted, query/filter region for operational lists, and content area with explicit states. The timeline is the primary order-transparency component. Customer-visible and internal notes are visually distinct. Admin and Agent data-heavy pages use compact, server-driven DataGrid queries; use detail drawers for inspection, but use explicit pages or dialogs for state-changing flows needing context. Destructive or irreversible actions require confirmation.

Credential reveal requires an intentional action and an audit entry. Inventory secrets never appear in grids or previews.

## Realtime UX

`AppShell` owns one connection. Components subscribe only for their active concerns and safely unsubscribe/dispose. Reconnect behavior must not duplicate handlers. Realtime events refresh only affected query data: notification counts, order status/timeline, tickets, assignments, customer-action state, and relevant dashboard counters. Avoid full-page reloads.

SignalR never replaces API authorization or database truth. Show non-blocking connection/recovery feedback only when it helps the user understand stale/reconnecting data.

## Required States for Data-Driven Pages

Every data-driven page defines:

- loading (skeleton or inline refresh),
- empty (with a meaningful next action where allowed),
- error (with retry/recovery),
- forbidden/unauthorized (without leaking data),
- stale or reconnecting state when realtime applies,
- success feedback for completed actions, and
- disabled-action reason when an action is unavailable.

## Responsive Behavior

The shell sidebar collapses at smaller widths. Preserve a consistent top bar, page header, spacing, and type scale. Prefer horizontal DataGrid scrolling over unreadable compression; stack form actions only when required. Keep actions discoverable and avoid hiding essential state behind hover-only controls.

## Delivery Priorities

**Current foundation:** shared AppShell, role navigation, catalog/checkout/order/payment flows, operational admin/agent pages, support tickets, notifications, and targeted realtime refresh.

**Remaining UI gaps:** inventory management/reservation/delivery surfaces and the operational reporting/analytics experience that follows them.

**Recommended order:** inventory UI after reservation/delivery contracts are settled → operational reports and analytics → integration-test-backed interaction hardening → deployment-readiness polish.
