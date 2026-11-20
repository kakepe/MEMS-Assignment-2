# Phase 2 engineering checkpoint

Verified November 15, 2026 (Pacific). Migration identifiers use EF's UTC timestamps, which are November 16. This is a local verification record, not the final assignment report.

The existing dedicated container ID `90a7678477b03e92a6c2adf378c13e36caeb13b27b6af1781430bf1699bc2cc9`, volume `mems-assignment2-sql-data`, database `MEMSAssignment2`, and SQL development secret were retained. Before migration, a read-only SQL check confirmed zero application tables. Both fresh SQL Server migrations were reviewed before applying. No old SQLite migration/database was imported; no existing volume/container was recreated.

| Verification | Result / evidence |
| --- | --- |
| Seven .NET 10 projects | Final build passed, zero warnings and errors; no eighth project |
| Dependency audit | All seven projects reported no known vulnerable packages, including transitives, against the current NuGet advisory feed |
| SQL migration/schema | Two migrations applied; 12 tables including history; intended foreign keys, constraints, filtered unique indexes and row versions present |
| Seeded domain data | Five named tenants, three positions and six candidates per tenant: 15 positions and 30 candidates |
| Identity | Five required accounts, seven roles, correct canonical tenant assignments and memberships; salted password hashes verified through UserManager |
| Tenant access in Data | Default context returns no domain rows; all five scopes return only their own records; foreign direct IDs return none; foreign inserts rejected |
| SQL negative cases | 20 constraint tests passed, including duplicate/invalid slugs, seat limits, cross-tenant candidate/position pairing, FKs, role-scope elevation, administrator assignment, lengths, contact values and filenames |
| Restrictive deletes | SQL Server refused deletion of referenced positions/municipalities inside rollback-only tests |
| Seed preservation | Edits/deletion of seeded samples and changed seeded-account password retained through repeat seeding; test mutations rolled back; whole-record fingerprint restored |
| Lockout | Five access failures caused Identity lockout inside a rollback-only test |
| Idempotent startup | Repeated migration/seed initialization changed no seeded record or password hash |
| Actual restart persistence | An owned candidate and owned administrator with a changed password survived restart of SQL Server and Aspire; contents/date/row version/hash and complete probe fingerprint unchanged |
| Fixture cleanup | Only owned probe rows removed; complete pre-probe record fingerprint restored |
| Browser regression | Phase 1 Admin, Public, API/OpenAPI/health, gateway routing and dashboard logs/traces passed with the Phase 2 database |
| Browser secret boundary | SQL password, Identity seed password and dashboard token absent from tested public responses/configuration; no Data/EF/server ServiceDefaults dependency in WASM |
| Original source safety | All 2,732 Assignment 1 files matched SHA-256, size and last-write baseline |
| Existing SQL secret | Byte-for-byte hash matched the Phase 2 start baseline |

End state: verification application processes were stopped; SQL container state is exited. Its original ID, persistent volume, seeded database and secrets remain intact. Restart with `scripts/start.ps1 -NoBuild`; no volume recreation or data import is needed. Original profile comparison also confirmed all 24 adapted biographies/keys are preserved (normalizing C# line endings), and all 24 copied JPEGs are byte-identical to the read-only source.

Commands exercised:

```powershell
dotnet restore MEMS.sln --verbosity minimal
dotnet build MEMS.sln --no-restore --verbosity minimal
.tools/dotnet-ef/dotnet-ef migrations add InitialSqlServerIdentityTenancy --project src/MEMS.Data --startup-project src/MEMS.Data --context MemsDbContext --output-dir Migrations --no-build
.tools/dotnet-ef/dotnet-ef migrations add FixHyphenatedSlugsAndAssetNames --project src/MEMS.Data --startup-project src/MEMS.Data --no-build
.tools/dotnet-ef/dotnet-ef migrations script --idempotent --project src/MEMS.Data --startup-project src/MEMS.Data --no-build --output .local/phase2-reviewed-migration.sql
dotnet run --project src/MEMS.AppHost --no-build --launch-profile https -p:_AspireCliRunHookSuppressed=true
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode PrepareRestart
docker restart mems-assignment2-sql
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode CompleteRestart
node scripts/inspect-dashboard.cjs
node scripts/verify-phase1.cjs
```

Database assertions compile into the existing API executable through a linked source file. They are local command-line modes guarded by Development and the intended database name, not HTTP endpoints. Detailed results: `artifacts/phase2/verify-phase2.json`, `prepare-phase2-restart.json`, `complete-phase2-restart.json`, `schema-check.txt`, plus connectivity evidence. Private restart state/passwords stay in ignored `.local`, never in result JSON.

Issues and fixes:

- Sandbox NuGet access required approved execution; EF CLI was installed workspace-locally, not globally.
- Adding the server framework reference made the explicit HealthChecks package redundant; it was removed to eliminate NU1510.
- Migration review bounded Identity key lengths to avoid oversized SQL composite indexes before the initial migration was applied.
- SQL LIKE interprets a trailing hyphen differently from the .NET filename regex. Initial seed rejected a valid logo and rolled back atomically. A reviewed forward migration put the hyphen first in the slug/photo/logo character classes. No database reset or data deletion was used.
- PowerShell's connection-builder adapter required canonical keyword indexers rather than dotted aliases; the test launcher was corrected without logging credentials.

No Phase 2 runtime blocker remains. Before Phase 3, enforce principal/tenant authorization at API boundaries and expose privileged Data operations only through explicit authorized services. Legacy role names must not substitute for ApplicationUser.MunicipalityId. Keep the experimental gateway package pinned; the temporary `/mems-public/` hosting mount still needs final tenant-route mapping. Instructor clarification about iOS consumption remains pending; no JWT, deployment or COMP4977 changes were made.
