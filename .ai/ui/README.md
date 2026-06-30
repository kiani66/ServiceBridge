# ServiceBridge UI Notes

The ServiceBridge UI supports digital service selection, dynamic order data collection, payment, fulfillment tracking, and admin or agent operations.

## Current Direction

- Blazor Web App foundation exists in `ServiceBridge.Web`.
- Temporary Razor Pages remain during migration.
- New UI should follow `.ai/UI_GUIDELINE.md`.

## UI Principles

- Make service and variant selection clear.
- Make IRR pricing and payment state clear.
- Treat credential inputs as sensitive.
- Show timeline and fulfillment state in plain language.
- Keep business rules out of components and pages.
