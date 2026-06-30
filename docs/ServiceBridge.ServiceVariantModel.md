# Service Variant Model

## PR4C1 rename

`ServicePlan` was renamed to `ServiceVariant`.

PR4C2A added the first minimal structured variant fields:

- `BillingCycle`
- `AccountDeliveryType`

PR4C2B completes the ServiceVariant v1.0 production fields:

- `CurrencyCode`
- `RegionCode`
- `FulfillmentMode`

`CurrencyCode` is stored as a required ISO-style code and defaults to `USD`.
`RegionCode` is stored as a required uppercase operational region code and
defaults to `GLOBAL`. `FulfillmentMode` uses the Domain `FulfillmentMode` enum
and defaults to `ManualAgent`.

Checkout behavior is intentionally unchanged in PR4C2B. ServiceBridge v1.0
checkout supports USD to IRR pricing only, so active purchasable variants are
validated as `CurrencyCode = USD`.

## Renamed model surface

- Domain type: `ServiceVariant`
- DbSet: `ServiceVariants`
- EF configuration: `ServiceVariantConfiguration`
- Digital service navigation: `Variants`
- Order FK: `ServiceVariantId`
- Order navigation: `ServiceVariant`
- Order snapshots: `VariantNameSnapshot`, `VariantDurationDaysSnapshot`,
  `VariantRegionCodeSnapshot`
- Structured fields: `BillingCycle`, `AccountDeliveryType`, `CurrencyCode`,
  `RegionCode`, `FulfillmentMode`

## Data preservation strategy

The migration `RenameServicePlansToVariants` renames the existing
`ServicePlans` table to `ServiceVariants` and renames the existing order FK and
snapshot columns in place. Existing rows are preserved, primary keys are not
regenerated, and order references continue to point at the same IDs.

The migration recreates affected SQL Server constraint, index, primary-key, and
foreign-key names under the new variant terminology.

The migration `AddServiceVariantBasicStructuredFields` adds non-null
`BillingCycle` and `AccountDeliveryType` integer columns to `ServiceVariants`.
Existing rows are preserved and receive safe compatibility defaults:

- `BillingCycle.None`
- `AccountDeliveryType.NotApplicable`

The migration `CompleteServiceVariantFields` completes the v1.0 variant fields
without recreating the table. Existing rows are preserved and receive safe
production defaults:

- `CurrencyCode = USD`
- `RegionCode = GLOBAL`
- `FulfillmentMode = ManualAgent`

## Compatibility notes

Some external API and UI compatibility terms still use plan terminology:

- Existing admin catalog routes remain `/plans`.
- Existing service detail responses still expose the `Plans` collection.
- Existing order and pricing request/response DTOs still use `PlanId` and
  `Plan*` fields.

Those compatibility fields are mapped internally from `ServiceVariant` and
`Variant*Snapshot` data. They should be treated as legacy wire names until a
separate contract-versioning PR changes them.
