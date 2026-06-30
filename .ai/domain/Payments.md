# Payments

## Rules

- Payment callback data must be verified before confirming payment.
- An order becomes `Paid` only after successful verification.
- Payment verification is part of the trust boundary.

## Guidance

- Keep payment integration details in Infrastructure.
- Keep payment use-case orchestration in Application.
- Represent expected payment failures with `ServiceResult` where possible.
- Preserve enough data for audit and support without exposing sensitive payment details unnecessarily.
