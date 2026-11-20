# MEMS Assignment 2 — Phase 7 documentation review

Team: **Abdullah Rehan and Alex Fisher**. The complete implementation through Phase 6 is preserved. Phase 7 documentation, fresh verification and publishing/package plans are prepared for review; no repository has been initialized or published and no final submission ZIP has been created.

Start with the [final documentation pack](docs/final/README.md), [review report PDF](docs/final/MEMS-Assignment-2-Final-Report.pdf), [read-only audit](docs/phase7-audit.md), [verification matrix](docs/final/verification.md), [100-mark readiness checklist](docs/final/rubric-and-readiness.md) and [publishing/package plan](docs/final/publishing-and-package-plan.md). Actual Codex use is documented in concise phase summaries; Copilot activity and written instructor feedback remain unverified. Original Phase 1–6 prompt files are unchanged.

The public Blazor WebAssembly website provides municipality discovery, root tenant routes, election information, position and candidate directories/profiles, anonymous photographs, search, filters, sorting and pagination. It consumes the existing tenant-aware REST API through the Aspire gateway. The complete Blazor Server Admin and SQL Server/Identity foundation are preserved. Exactly seven .NET 10 projects are included in MEMS.sln. Assignment 1 remains a read-only reference.

Public routes, API integration and usage: [docs/phase5-public.md](docs/phase5-public.md). Phase 5 verification: [docs/phase5-verification.md](docs/phase5-verification.md). Exact file inventory: [docs/phase5-files.txt](docs/phase5-files.txt).

Phase 6 adds anonymous personal voting guides at `/{slug}/guide`, native browser Session Storage, local add/remove/clear controls and Letter/A4 printing. Choices stay inside the public browser tab and never go to APIs, SQL, cookies, server sessions or URLs. Usage/privacy and verification: [docs/phase6-guide.md](docs/phase6-guide.md), [docs/phase6-verification.md](docs/phase6-verification.md), [docs/phase6-files.txt](docs/phase6-files.txt).

Current schema/ERD: `docs/phase2-schema.md`. Sample accounts/content: `docs/phase2-seeds.md`. Phase 2 results: `docs/phase2-verification.md`; exact changes: `docs/phase2-files.txt`.

Current API endpoints, JSON examples and security/client-routing contract: [docs/phase3-api.md](docs/phase3-api.md). Phase 3 results: [docs/phase3-verification.md](docs/phase3-verification.md). Exact Phase 3 source changes: [docs/phase3-files.txt](docs/phase3-files.txt).

Current Admin routes, usage, authentication/circuit design and tests: [docs/phase4-admin.md](docs/phase4-admin.md). Phase 4 verification: [docs/phase4-verification.md](docs/phase4-verification.md). Exact Phase 4 file inventory: [docs/phase4-files.txt](docs/phase4-files.txt).

## Run

Prerequisites: .NET SDK 10.0.402, Docker Desktop using Linux containers, installed Aspire CLI 13.6.4 and its runtime bundle, and an already trusted ASP.NET development certificate.

From this workspace:

```powershell
dotnet restore MEMS.sln
dotnet build MEMS.sln --no-restore
docker start mems-assignment2-sql
.\scripts\start.ps1 -NoBuild
```

If PowerShell blocks script execution, use the equivalent command directly:

```powershell
dotnet run --project src/MEMS.AppHost --no-build --launch-profile https -p:_AspireCliRunHookSuppressed=true
```

The direct .NET command remains a fallback. The normal startup script now uses the professor-required Aspire CLI command, verified with the existing trusted development certificate:

```powershell
aspire run --apphost src/MEMS.AppHost/MEMS.AppHost.csproj --no-build --launch-profile https --non-interactive --nologo
```

No certificate installation/trust change was needed. If the existing trusted certificate is unavailable, obtain approval before changing certificate settings. Keep the current SQL container and resource configuration; do not use Aspire isolated mode for this retained database.

| Service | Local URL |
| --- | --- |
| Admin dashboard / login entry | https://localhost:7252/admin |
| Public municipality discovery | https://localhost:7290/ |
| Public tenants | /burnaby, /surrey, /richmond, /pitt-meadows, /maple-ridge on port 7290 |
| Personal guide | https://localhost:7290/burnaby/guide (also every other tenant's `/guide`) |
| API temporary probe | https://localhost:7192/api/foundation |
| API readiness / liveness | https://localhost:7192/health and /alive |
| API OpenAPI | https://localhost:7192/openapi/v1.json |
| API readable documentation | https://localhost:7192/api/docs |
| Public municipality directory | https://localhost:7192/api/municipalities |
| Aspire dashboard | https://localhost:17048 |

Use the authenticated dashboard login URL printed in the launching terminal. It contains a temporary token; do not publish it. Public tenant routes now start at the gateway root. The old `/mems-public/` entry and legacy tenant deep links redirect to their root equivalents. Preserve the existing ignored development secrets; ordinary restarts need no seed reconfiguration.

The existing gateway API transport is `https://localhost:7290/mems-public/api/mems-api/api/...`. For example, append `/municipalities` to that API base. Canonical `/api/...` media URLs in DTOs must use the same transport prefix when fetched from the gateway. The Admin uses an authenticated local media proxy; the API remains suitable for future JSON clients.

## Stop and restart

Press Ctrl+C in the AppHost terminal. Its application processes stop. The persistent SQL container may remain running by design. To stop only this project's database without removing its container or volume:

```powershell
docker stop mems-assignment2-sql
```

Restart Docker Desktop if needed, run `docker start mems-assignment2-sql`, then rerun `scripts/start.ps1 -NoBuild`. The current container was retained with this restart order. Aspire's Persistent lifetime permits replacement on configuration changes; the original Phase 2 container was replaced during the first Phase 3 launch while its existing volume/database remained intact. See the explicit exception in `docs/phase3-verification.md`. Never use container/volume removal or prune commands for this workflow.

## Storage and packages

- Container: `mems-assignment2-sql`; image: `mcr.microsoft.com/mssql/server:2022-CU24-ubuntu-22.04`.
- Volume: `mems-assignment2-sql-data`, mounted at `/var/opt/mssql`.
- Database: `MEMSAssignment2`; 12 tables including Identity, election entities, seed history and EF migration history. API alone applies reviewed Code First migrations and one-time development seeds before becoming ready. No EnsureCreated, database reset, or SQLite import is used.
- Identity development seed credential: ignored `.local/identity-seed.json`. The configuration helper retains an existing file; on a fresh workspace it prompts for the rubric's demo password. Retain it locally and never commit it. Existing account password changes are not overwritten during startup.
- A random local-development SQL password is created once in ignored `.local/apphost-secrets.json`. Retain this file with the volume. Do not print, publish, or commit it. No password is hardcoded in source or delivered to WASM.
- NuGet packages and Aspire templates are downloaded into ignored `.tools/`; no global template installation was used. Installed Aspire tooling also maintains its own runtime/cache/log locations.
- CLI, AppHost SDK and templates: 13.6.4. Explicit Hosting.AppHost/SqlServer patch: 13.6.5, because Hosting 13.6.4 is affected by GHSA-f7vh-h5g3-7jrh. Compatibility with the installed CLI was verified by build and runtime checks.
- Blazor gateway package: `13.6.4-preview.1.26523.11`, matching the installed tool commit. The integration is experimental; its narrowly scoped compiler diagnostic is acknowledged in AppHost.cs.
- EF Core and WASM: 10.0.13; cryptography transitive dependency is explicitly patched to 10.0.14.
- SkiaSharp 4.153.1 validates decoded JPEG/PNG uploads and re-encodes them to generated PNG files. Uploads live in ignored `.local/media/tenants/`, separate from configuration/keys. Existing seed images are served only by ownership-scoped media endpoints; raw static image paths are disabled.
- Admin and API reference server ServiceDefaults. WASM references Shared only. The gateway forwards only API routes, not SQL references. Browser telemetry proxying is disabled in this phase; gateway/API/Admin telemetry is enabled.

## Verify

After Admin login, `/admin/connectivity` has the retained `Check API connection` button. The public `/_status` page has `Check availability`. Admin success displays `API connected. SQL Server connected.`; public success displays `Election information services are available.` Admin uses Aspire HTTP service discovery; Public uses HTTPS on its gateway origin and omits authentication credentials for all API/media requests. No CORS wildcard is configured.

Automated checks use the installed Chrome or Edge, without downloading another browser:

```powershell
npm install --prefix .tools/browser --cache .tools/npm-cache --save-exact @playwright/test@1.65.1
```

For the current verification workflow, redirect AppHost output to `.local/apphost-direct.log`, then in another terminal run:

```powershell
node scripts/inspect-dashboard.cjs
node scripts/verify-phase1.cjs
dotnet list MEMS.sln package --vulnerable --include-transitive --no-restore
```

`inspect-dashboard.cjs` discovers running URLs and logs in to the dashboard without printing its token. Verification checks real Admin interactivity, WASM loading/reloading, same-origin API calls, SQL-backed readiness, OpenAPI, browser configuration privacy, the browser dependency graph, and dashboard logs/traces. Screenshots and machine-readable results are under ignored `artifacts/phase1/`.

`scripts/check-assignment1.py` reads the reference tree and compares all files against an ignored SHA-256/size/last-write baseline. It never writes into Assignment 1. The verified baseline includes 2,732 files, including its databases and .git contents. Use an existing Python interpreter with `-B scripts/check-assignment1.py verify`; do not replace the baseline.

See `docs/phase1-verification.md` for observed results and `docs/phase1-files.txt` for the exact source file inventory. Build/cache/runtime outputs are separately identified there.

## Phase 2 database verification

With the stack/database running, use:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1
```

The initial seed checks expect the freshly seeded demo accounts. Constraint tests and sample edit/deletion/password/lockout tests use rollback-only transactions, followed by full record fingerprint comparisons. SQL identity counters and rowversion allocation counters can advance in rolled-back negative tests; existing records are restored unchanged.

Actual restart verification uses owned fixtures, never repurposes existing accounts, and requires this order:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode PrepareRestart
# Stop AppHost with Ctrl+C. Restart the same SQL container if testing disk persistence:
docker restart mems-assignment2-sql
.\scripts\start.ps1 -NoBuild
# In another terminal after readiness:
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode CompleteRestart
```

The completion mode verifies the committed probe fingerprint and changed password, then removes only its owned fixtures. Keep its ignored state file until completion; do not manually delete application data to resolve a failed probe.

## Phase 3 API verification

With Aspire running, execute in this order:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode PreparePhase3
node scripts/verify-phase3.cjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode CompletePhase3
node scripts/verify-phase3-browser.cjs
```

The suite signs in with legitimate Identity credentials. It tests only owned CRUD records and a temporary normal Identity account for lockout; existing accounts/passwords are preserved. Completion removes the owned account and verifies the pre-test election/Identity record fingerprint. Keep ignored fixture state/ledger files until cleanup succeeds. Results contain no passwords/tokens and live under `artifacts/phase3/`. Verification CLI modes are Development-only, database-name guarded and not HTTP authentication endpoints. Full test coverage, limitations and recovery guidance are in `docs/phase3-api.md` and `docs/phase3-verification.md`.

EF CLI 10.0.13 is installed workspace-locally at `.tools/dotnet-ef`. To reproduce the reviewed SQL without connecting to a database:

```powershell
.tools/dotnet-ef/dotnet-ef migrations script --idempotent --project src/MEMS.Data --startup-project src/MEMS.Data --no-build --output .local/phase2-reviewed-migration.sql
.tools/dotnet-ef/dotnet-ef migrations has-pending-model-changes --project src/MEMS.Data --startup-project src/MEMS.Data --no-build
```

The design-time factory deliberately uses a non-routable design-only host when no explicit connection is supplied; it never falls back to LocalDB/SQLite. Do not run database-update tooling against an arbitrary connection. Existing migration `Down` methods were not executed. Migrations and seed initialization are restricted to Development and the MEMSAssignment2 database.

## Scope remaining

Guides and printing are implemented and verified. The rubric's page 6 Local Storage wording conflicts with its explicit Session Storage privacy requirements on pages 7–8; the implementation follows the latter and the marking guide. No voting choices or vote counts are accepted or stored by the API or SQL. Browser session restore and opener/duplicated tabs may restore/copy Session Storage under normal browser behavior; Clear guide removes the active tenant's choices explicitly. No selection count cap is imposed because neither assignment PDF explicitly requires one. Phase 7 documentation is prepared for review; commits, GitHub publishing, the final ZIP and submission remain subject to explicit approval. No Azure deployment is required. The instructor's iOS integration question remains unresolved; no JWT or COMP4977 changes were assumed.

The Assignment 1 mark was 97/100; the reported deduction was a missing ERD in the report. Include the updated Phase 2 ERD in the later final report. AI assistance for these phases was OpenAI Codex. Copilot instruction/prompt files are included, but no GitHub Copilot usage is claimed. No Git commands, Azure actions, repository creation, final report, or source ZIP were performed.
