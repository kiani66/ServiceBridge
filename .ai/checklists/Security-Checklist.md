# Security Checklist

- Credentials are encrypted at rest.
- Plaintext credentials are not logged.
- Sensitive values are redacted from diagnostics.
- Payment callbacks are verified before confirming payment.
- Authorization is checked before credential or fulfillment access.
- JWT and Cookie auth behavior remains compatible.
