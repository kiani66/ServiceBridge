# Service Variant Model

## PR4C1 rename

`ServicePlan` was renamed to `ServiceVariant`.

This PR is a mechanical/domain rename only. It does not add structured variant
fields such as `BillingCycle`, `AccountDeliveryType`, or fulfillment-mode
fields to the entity. Those fields are intentionally deferred to a later PR.

## Renamed model surface

- Domain type: `ServiceVariant`
- DbSet: `ServiceVariants`
- EF configuration: `ServiceVariantConfiguration`
- Digital service navigation: `Variants`
- Order FK: `ServiceVariantId`
- Order navigation: `ServiceVariant`
- Order snapshots: `VariantNameSnapshot`, `VariantDurationDaysSnapshot`,
  `VariantRegionCodeSnapshot`

## Data preservation strategy

The migration `RenameServicePlansToVariants` renames the existing
`ServicePlans` table to `ServiceVariants` and renames the existing order FK and
snapshot columns in place. Existing rows are preserved, primary keys are not
regenerated, and order references continue to point at the same IDs.

The migration recreates affected SQL Server constraint, index, primary-key, and
foreign-key names under the new variant terminology.

## Compatibility notes

Some external API and UI compatibility terms still use plan terminology:

- Existing admin catalog routes remain `/plans`.
- Existing service detail responses still expose the `Plans` collection.
- Existing order and pricing request/response DTOs still use `PlanId` and
  `Plan*` fields.

Those compatibility fields are mapped internally from `ServiceVariant` and
`Variant*Snapshot` data. They should be treated as legacy wire names until a
separate contract-versioning PR changes them.
