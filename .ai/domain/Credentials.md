# Credentials

## Rules

- Credentials must be encrypted at rest.
- Credentials are sensitive fulfillment data.
- Access must be limited to authorized users and fulfillment paths.

## Guidance

- Keep encryption implementation in Infrastructure.
- Do not log plaintext credentials.
- Do not expose credentials in broad API or UI DTOs.
- Redact credentials in diagnostics, audit views, and support screens unless explicit authorization exists.
