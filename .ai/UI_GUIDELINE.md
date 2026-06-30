# UI Guideline

ServiceBridge UI work should support a digital service mediation and payment workflow. It should not feel like a generic shopping cart or retail storefront.

## Product Tone

- Clear, operational, and trust-oriented.
- Optimized for service selection, checkout, payment status, and fulfillment tracking.
- Avoid e-commerce language unless it maps directly to the current domain model.

## Blazor Direction

- The Blazor Web App foundation exists in `ServiceBridge.Web` alongside temporary Razor Pages.
- New UI work should follow the Blazor foundation unless a task explicitly targets existing Razor Pages.
- Preserve temporary Razor Pages until their replacement path is explicit.

## UI Rules

- Keep pages focused on the user workflow.
- Do not move business logic into UI components.
- Use shaped DTOs and application services.
- Do not bind UI directly to EF entities.
- Show payment and fulfillment states clearly.
- Treat credential fields as sensitive.

## Validation

For UI PRs, run the solution build and tests. When practical, verify the relevant page or component manually.
