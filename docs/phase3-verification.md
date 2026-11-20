# Phase 3 engineering checkpoint

Implemented and verified November 15, 2026 (Pacific). This is the Phase 3 engineering record, not the final assignment report. Stop here pending Phase 4 approval. Exactly seven application projects remain. No Blazor login/CRUD screens, finished public pages, voting selections, printing, JWT, Git operations or deployment were added.

The implementation covers the rubric's Municipality/Position/Candidate REST operations, shared contracts, server-enforced multi-tenancy, Identity permissions, image handling, validation, error handling and API documentation. It retains the approved Blazor Server Admin / standalone WASM Public / API / Data / Shared / ServiceDefaults / AppHost architecture. Existing EF tenant filters, query reader, write guards, composite foreign keys, unique indexes and restrictive deletes are reused. No schema change or migration was needed. Full UI and final submission evidence remain later-phase requirements; no complete rubric score is claimed.

## Material infrastructure exception

The original Phase 2 SQL container ID was `90a7678477b03e92a6c2adf378c13e36caeb13b27b6af1781430bf1699bc2cc9`. Final inspection found container `mems-assignment2-sql` now has ID `e7fd02edfdaa499fb8c6ca179b91c3e680cd069787270457bdda608932ec7a26`, created at November 15, 19:43:19 PST during the initial Phase 3 Aspire launch. The original ID is no longer present. No explicit container-removal command was issued. The timing indicates Aspire-managed replacement; retained diagnostics do not establish its exact lifecycle trigger. Therefore the requirement to preserve the original container instance was not fully met.

The original named volume `mems-assignment2-sql-data` remains mounted at `/var/opt/mssql`. `MEMSAssignment2`, both migration records, seed data, administrator accounts/password hashes and both development secret files were preserved. The full compared election/Identity record fingerprint matched after API test cleanup and also matched the original completed Phase 2 checkpoint. No volume deletion, database reset, SQLite import or password reset occurred. The replacement instance retained its ID across the verified SQL/Aspire restart and an additional explicit Docker stop/start followed by Aspire. See `artifacts/phase3/sql-status.json` for observed identities and final state. The old container ID cannot be restored by renaming the current container.

Aspire documents that persistent containers can be recreated when configuration changes; a persistent volume separately protects the data. This explains the relevant behavior, not a proven trigger for this run: [Aspire resource lifetimes](https://aspire.dev/app-host/resource-lifetimes/). Before future container configuration/package changes, review this reuse behavior rather than assuming Persistent guarantees an immutable instance. The tested restart order explicitly starts the existing container before launching Aspire.

## Changes

Exact source/configuration file inventory: [phase3-files.txt](phase3-files.txt). Complete routes, JSON models/examples and client instructions: [phase3-api.md](phase3-api.md).

- Shared request/response models with DataAnnotations, HTTP(S) website validation, bounded search/pagination and opaque row-version strings; no database entities or Identity records in API responses.
- Principal-aware Data access resolves URL slugs, re-reads Identity scope/assignment/roles and creates tenant-scoped contexts. The public directory is the narrow deliberate discovery exception. System municipality CRUD is Super Admin only. Client tenant IDs, role headers and submitted foreign IDs cannot select an unauthorized context.
- Controllers expose all five REST operations for municipalities, positions and candidates, tenant media, cookie sign-in/session/logout and CSRF token issuance. Optimistic concurrency requires a current quoted If-Match for PUT/DELETE/media changes.
- JPEG/PNG decoding, format/size/dimension checks and sanitized PNG output through SkiaSharp 4.153.1. Uploads are tenant-owned under ignored `.local/media/tenants`; temporary request buffering is under `.local/http-temp`. Seed files are allowlisted and never removed by upload cleanup. Raw static image serving is disabled.
- Identity's existing SQL password hashes and keys are reused. Secure HttpOnly cookies, principal-bound CSRF, unsafe-request origin checks, role/tenant policies, zero-interval security-stamp validation, lockout and sign-in throttling protect administrative operations. No external CORS allowance is enabled. Generic Problem Details and sanitized structured logging prevent exception/SQL/path/credential leakage.
- Built-in OpenAPI includes safe models, request headers, security schemes and response codes; `/api/docs` provides readable usage without an external script/CDN dependency. Verification compiles into the existing API executable and uses legitimate Identity sign-in over HTTP; no eighth project or test authentication endpoint exists.

## Verification results

| Check | Observed result / evidence |
| --- | --- |
| Restore | Required dependencies restored successfully; network access required approved execution |
| Seven-project build | `dotnet build MEMS.sln --no-restore --verbosity minimal`: success, 0 warnings, 0 errors |
| Model/migrations | EF `has-pending-model-changes`: no changes; existing two SQL Server migrations retained unchanged |
| Package audit | All seven projects reported no known vulnerable direct/transitive packages against NuGet's current advisory feed |
| API authentication/authorization | Real cookies and current CSRF tokens; all five rubric accounts sign in; anonymous writes 401; foreign tenant/system-admin operations 403; scoped foreign IDs 404 |
| Tenant forgery | Forged headers, JSON tenant/internal fields, query tenant IDs, foreign candidate/position IDs and foreign submitted parents cannot cross scope |
| REST CRUD | Municipality Super Admin CRUD, assigned municipality position/candidate CRUD including deletes, and Super Admin cross-tenant administration passed on owned fixtures |
| Validation/concurrency | Required/length/contact/date/seat/slug errors 400; duplicate names/slugs, stale versions and dependent deletes 409; missing/malformed If-Match 400, including a lone double-quote character |
| Candidate querying | Name substring search, position filter, invalid foreign filter, stable tie ordering, page/page-size bounds and pagination passed |
| Media rejection | Unsupported SVG, fake image bytes, extension/decoded-format mismatch, path filename, empty image and excessive dimensions return 400; 2 MiB + 1 byte returns 413 |
| Media acceptance/ownership | Real JPEG with an untrusted MIME label and valid PNG accepted, decoded/re-encoded to PNG; trailing marker removed; foreign tenant 403/foreign record 404; replace/delete and Super Admin logo operations passed; seeded portrait remains accessible |
| CSRF/origin/CORS | Missing, anonymous-principal and another-user tokens rejected; untrusted modifying origin 403; no cross-origin allowance; HTTPS required for sign-in/modifications |
| Identity behavior | Legitimate logout invalidates session; temporary normal account locks after five failed passwords; seeded passwords not reset; secure HttpOnly cookies verified |
| Errors/privacy | Problem Details 400/401/403/404/409/413 observed; in-process generic 500 test suppresses private exception marker/type; DTOs exclude password hashes, stamps, EF internals and asset paths |
| Browser gateway security | Real browser sign-in/CSRF, own-tenant data/image, foreign-tenant 403, logout and JavaScript-inaccessible cookies passed through existing Aspire gateway |
| Aspire | Dashboard opened/authenticated; SQL/database, API, Admin, Public and managed gateway resources Running; structured logs and API traces visible |
| Phase 1 regression | Admin Interactive Server button, WASM load/reload, same-origin API/SQL readiness, OpenAPI and browser secret/dependency boundary all passed |
| Phase 2 regression | Five tenants, 15 positions, 30 candidates, five accounts, seven roles; EF tenant/write guards, all 20 SQL constraint negatives, repeat seeds, rollback-only password/lockout tests passed |
| Restart persistence | A committed owned candidate and owned account with a changed password survived actual SQL and Aspire restart, with contents/date/row version/hash and seed records unchanged; only owned fixtures removed afterward |
| Existing data | Before/after fingerprints of Municipalities, Positions, Candidates, AspNetUsers, AspNetRoles, AspNetUserRoles and SeedHistory matched after cleanup; the Phase 3 baseline also exactly matched the original completed Phase 2 checkpoint |
| Original source | All 2,732 Assignment 1 files match their SHA-256, length and last-write baseline, including its SQLite database and Git files |
| Development secrets | Existing `.local/apphost-secrets.json` and `.local/identity-seed.json` byte hashes unchanged |
| Container instance | Original instance was replaced during first Phase 3 launch: exception documented above; original volume/data preserved |

The fingerprint comparison concerns application records, not byte-identical SQL database files: SQL identity/rowversion allocation counters can advance in negative/rolled-back tests and owned CRUD operations. Authentication uses the actual existing development credentials without printing them. Lockout tests use a separately generated normal account. Fixture passwords, cookies, dashboard tokens and private baseline state stay in ignored local files or test-process memory, not public evidence.

Machine-readable evidence under ignored `artifacts/phase3/` includes `api-verification.json` (nine grouped HTTP checks), `browser-security.json` (four real-browser security checks), `database-preservation.json`, `final-checks.json`, `sql-status.json`, `assignment1-integrity.txt`, safe `examples.json` and copied earlier-phase regression results/screenshots. Tests were implemented as focused assertion scripts/linked CLI checks inside existing projects; no separate xUnit project was introduced. This is local integration evidence, not an independent penetration test or deployment validation.

## Commands exercised

```powershell
dotnet restore MEMS.sln --verbosity minimal
dotnet build MEMS.sln --no-restore --verbosity minimal
dotnet list MEMS.sln package --vulnerable --include-transitive --no-restore
.tools/dotnet-ef/dotnet-ef migrations has-pending-model-changes --project src/MEMS.Data --startup-project src/MEMS.Data --no-build

dotnet run --project src/MEMS.AppHost --no-build --launch-profile https -p:_AspireCliRunHookSuppressed=true
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode PreparePhase3
node scripts/verify-phase3.cjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode CompletePhase3
node scripts/verify-phase3-browser.cjs

powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode PrepareRestart
# Stop AppHost, then restart this same SQL container and start AppHost again.
docker restart mems-assignment2-sql
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode CompleteRestart
node scripts/inspect-dashboard.cjs
node scripts/verify-phase1.cjs
```

The Assignment 1 check used the existing Python interpreter with `-B scripts/check-assignment1.py verify`, reading the reference tree and writing results only here. Nothing was built/restored/run in Assignment 1.

Issues resolved within Phase 3: NuGet/Docker sandbox restrictions required approved execution; API model writes and tenant-linked role creation use appropriate EF retry/transaction boundaries; failed role-name collisions leave no partial tenant; a one-character quoted If-Match value now returns 400 instead of an exception. Final builds/tests pass. The remaining container-preservation exception is not hidden by these functional passes.

## Start, stop and Phase 4 attention

From `C:\Users\Asma\Documents\MEMS-Assignment-2`, with Docker Desktop running:

```powershell
docker start mems-assignment2-sql
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/start.ps1 -NoBuild
```

Use the authenticated dashboard login URL printed locally. Admin: `https://localhost:7252/admin`; Public scaffold: `https://localhost:7290/mems-public/`; API: `https://localhost:7192`; readable API docs: `https://localhost:7192/api/docs`; OpenAPI: `https://localhost:7192/openapi/v1.json`; dashboard: `https://localhost:17048`. The direct API GET directory is `/api/municipalities`.

Stop AppHost with Ctrl+C. To stop SQL without removing its container/volume, run `docker stop mems-assignment2-sql`. Start the same container before the next AppHost run. Do not remove/prune containers or volumes or clear seed markers. If a build is needed, stop AppHost first, run restore/build, then start. Both development secret files must stay with this workspace/volume.

Final observed state: AppHost/application verification processes stopped; the current SQL container is exited and retained with its original persistent volume/database. No active Phase 3 fixture account/state remains. This final state does not undo the earlier container-instance exception.

Before Phase 4: retain canonical stored tenant authorization when wiring Admin login/CRUD; refresh CSRF after authentication; carry current ETags; map canonical API media/Location paths through the gateway service prefix when applicable. Final tenant UI routes remain unfinished. Logo/municipality changes remain Super Admin only, matching the inherited municipality-management rule. Review the container-instance exception before infrastructure configuration changes. Instructor clarification about future iOS consumption remains pending; no JWT/COMP4977/Azure action is assumed. No other build/API functional blocker was observed.
