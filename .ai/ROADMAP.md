# Roadmap

ServiceBridge is moving in small, reviewable PRs. Preserve compatibility while the Blazor Web App foundation grows alongside temporary Razor Pages.

## Current Baseline

- PR #6: Variant-aware Pricing Resolver merged.
- PR #7: Variant-based Order Creation merged.
- PR #8: Blazor UI Foundation + Design System merged.
- PR #9A: AI Operating System Foundation.

## Planned Sequence

- PR #9B: UI Design System.
- PR #10: UI Prototype with mock data.
- PR #11: Public Catalog UX on Blazor.
- PR #12: Checkout / Dynamic Order Form.
- PR #13: User Order Tracking.
- PR #14: Payment Hardening / UX.
- PR #15: Agent Dashboard.
- PR #16: Admin Operations + Audit.

## Roadmap Rules

- Keep each PR narrowly scoped.
- Prefer vertical slices that can be reviewed and validated independently.
- Do not mix schema, UI, and domain changes unless the user explicitly requests that scope.
- Preserve the existing API and UI behavior unless the PR explicitly changes it.
