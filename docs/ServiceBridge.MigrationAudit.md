# ServiceBridge Migration Audit

## Migration Summary

ServiceBridge now contains the committed MVP snapshot migrated into the ServiceBridge repository and renamed across solution, projects, namespaces, settings, startup configuration, and tests. The root project layout is intentionally preserved for this cleanup PR to avoid architecture churn.

No business behavior was intentionally changed in this audit. The cleanup focused on rename hygiene, configuration placeholders, architecture reference verification, EF migration review, and documentation.

## Verified Items

- Solution and project names use ServiceBridge naming.
- Namespace declarations and using statements compile under ServiceBridge namespaces.
- Project references follow the expected layered monolith boundaries.
- Swagger title is `ServiceBridge.Api`.
- Web cookie name is `ServiceBridge.Auth`.
- API HttpClient name is `ServiceBridgeApi`.
- API CORS policy name is `ServiceBridgeWebPolicy`.
- Launch profiles are named `ServiceBridge.Api` and `ServiceBridge.Web`.
- Database context is `ServiceBridgeDbContext`.
- EF model snapshot is `ServiceBridgeDbContextModelSnapshot`.
- Test namespaces use ServiceBridge naming.
- Development-only placeholder emails use `servicebridge.local` or `servicebridge.test`.
- Appsettings contain empty placeholders for connection strings, signing keys, and seeded passwords.

## Architecture Reference Result

The intended project dependency rules are:

- Domain has no project references.
- Application references Domain and Shared only.
- Infrastructure references Application, Domain, and Shared.
- Api references Application, Infrastructure, and Shared.
- Web references Application and Shared only.
- Tests may reference the projects needed for unit and web behavior coverage.

The Web project does not reference Infrastructure and does not directly access the DbContext.

## EF Migration Review

No new EF migration was created. Existing migration files were preserved and renamed only as part of the namespace/context migration. The migration IDs remain unchanged, and the model snapshot compiles as `ServiceBridgeDbContextModelSnapshot`.

Production review is still required before applying migrations to an existing database. A database that was previously migrated by the MVP assembly may already contain rows in `__EFMigrationsHistory`; deployment should confirm the migration assembly, migration IDs, and schema history before running automated migration commands.

## Startup Migration Note

The API currently calls `Database.Migrate()` during startup in all environments. This preserves current behavior, but it is risky for production because application startup can apply schema changes automatically. Before production deployment, decide whether migrations should be run by a controlled deployment step instead of app startup.

## Sensitive Configuration

No real secrets should be committed. Current configuration files use empty placeholders for sensitive values:

- `ConnectionStrings:DefaultConnection`
- `Jwt:SigningKey`
- `AdminSeed:Password`

Local secrets should remain in user secrets, environment variables, or deployment configuration, not in repository files.

## Remaining Migration Risks

- EF migration history must be reviewed against any existing production or staging database.
- Automatic startup migration should be reviewed before production deployment.
- The root project layout is intentionally kept for now; moving projects into `src/` or `tests/` should be a separate structural PR if still desired.
- Existing MVP business behavior is preserved, so product-level hardening work remains for later roadmap PRs.
