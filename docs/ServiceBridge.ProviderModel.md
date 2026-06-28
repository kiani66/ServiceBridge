# ServiceBridge Provider Model

## Why Provider Is an Entity

Provider is business-significant catalog vocabulary. It affects customer filtering, operator reporting, catalog grouping, and future fulfillment routing. Keeping it as arbitrary text on `DigitalService` makes duplicates such as `OpenAI`, `Open AI`, and empty provider values hard to govern.

PR4B introduces `Provider` as a first-class domain entity while preserving current catalog behavior.

## Relationship

`Provider` has a one-to-many relationship with `DigitalService`:

- `Provider.Id`
- `Provider.Name`
- `Provider.Slug`
- optional `WebsiteUrl`
- optional `LogoUrl`
- `Provider.IsActive`
- audit timestamps
- `Provider.Services`

`DigitalService` now stores required `ProviderId` and has a `Provider` navigation. Public catalog and admin responses still return provider name text for compatibility.

## Migration Strategy

The EF migration creates the `Providers` table, derives distinct provider names from the old `DigitalServices.Provider` text column, creates normalized slugs, assigns `DigitalServices.ProviderId`, and then removes the old text column.

Null or empty provider names are migrated to:

- Name: `Unknown Provider`
- Slug: `unknown-provider`

The migration is SQL Server compatible and preserves existing `DigitalServices` rows.

## Compatibility With Existing Inputs

Existing admin catalog create/edit DTOs still accept provider text. The application resolves provider text to a normalized slug, reuses an existing provider when possible, and creates a provider when needed. This keeps existing admin flows working without adding a Provider admin UI in this PR.

## Not In This PR

`ServicePlan` was not renamed to `ServiceVariant`.

This PR does not change pricing resolution, order creation, payment behavior, inventory, automation, tickets, notifications, wallets, or runtime catalog behavior beyond representing provider as a relationship.
