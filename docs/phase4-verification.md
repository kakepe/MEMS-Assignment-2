# Phase 4 engineering checkpoint

Verified November 15, 2026 (Pacific). Phase 4 implements the shared Blazor Server administrative application at `/admin`. This checkpoint is engineering evidence, not the final assignment submission report. Stop here pending explicit Phase 5 approval.

All 16 pages of the COMP4976 Assignment 2 PDF, including the 100-mark grading guide, were reviewed. The seven-project architecture and existing SQL Server/Identity/tenant foundation remain compatible with the rubric. The Assignment 1 dashboard and administration forms were inspected read-only to retain their fields, limits and workflows. The known Assignment 1 deduction remains the missing report ERD; no additional instructor code deductions are invented.

Full route/usage/security reference: [phase4-admin.md](phase4-admin.md). Exact created/modified source list: [phase4-files.txt](phase4-files.txt). The API contract remains documented in [phase3-api.md](phase3-api.md).

## Implemented administration

| Area | Delivered behavior |
| --- | --- |
| Account workflows | Existing Identity username/password login, logout confirmation, change password, safe errors, lockout, local return URLs and authorization-aware navigation |
| Circuit security | Revalidating authentication state, immediate fresh permission/stamp/session checks per operation, session expiry and logout revocation shared with API |
| Dashboard | Live database totals; Super Admin system-wide, municipality admins scoped to their single assignment |
| Municipalities | List/details/create/edit/delete, normalized slug, province/election date/description/public title/brand colour and logo upload/replacement/removal; tenant changes Super Admin only |
| Positions | Tenant selector, list/details/create/edit/delete, name/seat validation and restricted parent deletion |
| Candidates | Tenant/position selection, list/details/create/edit/delete, biographies/contact fields, portraits, upload/replacement/removal, name search, position filter, stable sorting and pagination |
| Shared usability | Bootstrap layout, sidebar/mobile navigation, cards, tables, labeled forms, field/summary validation, loading/empty/error states, success feedback, destructive-action confirmation and keyboard focus/skip link |
| Infrastructure | Protected connectivity page, existing Aspire discovery/health, structured Admin logs and traces, safe error/missing-page screens; Public WASM remains unchanged |

No new .NET project, application NuGet dependency, database table, migration, seed batch or administrator role was introduced. Exactly seven projects remain. Account screens render statically so HTTP responses can issue cookies; management components use Interactive Server. Per-operation scopes isolate Identity's scoped context and the existing `IDbContextFactory` queries from circuit lifetime. Data services and SQL constraints are reused rather than duplicated. The Admin media proxy forwards only the current Identity cookie to its own Aspire-discovered API; seed assets are not copied into another public static directory.

The implementation follows the account/static-rendering pattern in the installed .NET 10 Blazor template and Microsoft's guidance on [Blazor authentication](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0), [render modes](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0), and [EF Core context lifetimes](https://learn.microsoft.com/en-us/aspnet/core/blazor/blazor-ef-core?view=aspnetcore-10.0). Our additional local session registry revokes stale circuits without changing seeded users' database records during logout. It also bounds a connected session by its issuing cookie's expiry. Existing passwords/hashes and Identity's password-change/stamp behavior remain authoritative.

## Verification results

| Verification | Observed result |
| --- | --- |
| Workspace | All implementation and evidence writes stay in `C:\Users\Asma\Documents\MEMS-Assignment-2` |
| Restore/build | Restore reports all projects up-to-date; final seven-project build succeeds with 0 warnings and 0 errors |
| EF model | `has-pending-model-changes` reports none; both existing migrations and snapshot unchanged |
| Dependency audit | All seven projects report no known vulnerable direct/transitive NuGet packages against the current feed |
| Existing account login | `super`, `Burnabyadmin`, `Surreyadmin`, `Richmondadmin` and `pm` authenticate through real browser forms using existing credentials |
| Login validation | Empty/incorrect credentials yield safe messages; password inputs are cleared in returned forms; external return URLs stay local; missing form CSRF is rejected with 400 |
| Logout | GET only presents confirmation; POST requires antiforgery; protected navigation challenges after logout; an already-open circuit cannot save; replaying its prior cookie at the separate API returns 401 |
| Change password | A normal owned account verifies mismatched confirmation and incorrect current-password errors, successful change, refreshed login and login using the new password; no seeded password was changed |
| Stale circuits/expiry | Old circuit denied after password/stamp change and after logout; focused session checks reject past expiry and accept future expiry; ordinary HTTP tests use legitimate Identity authentication throughout |
| Lockout | The owned account remains unable to log in with its correct changed password after five failed login attempts |
| Dashboard scope | Baseline Super totals 5 municipalities / 15 positions / 30 candidates; municipal totals 1 / 3 / 6. Owned CRUD changes totals and cleanup returns them to baseline |
| Municipality UI CRUD | Super Admin creates, views, edits and deletes an owned tenant; slug normalized; election fields validated; dependent deletion blocked safely |
| Position UI CRUD | Super Admin and an assigned municipality admin create/view/edit/delete owned positions; zero seats and duplicates rejected; referenced position deletion blocked |
| Candidate UI CRUD | Super Admin and an assigned municipality admin create/view/edit/delete owned candidates; name/profile/contact/position forms and safe validation work |
| Tenant isolation | Municipal list contains only assigned tenant; no tenant-create link; direct tenant create/foreign slug/foreign candidate edit denied; foreign record form not exposed; unknown slug fails safely |
| Forged parent | A foreign position manually added to the browser's select options is rejected by the server before a candidate update |
| Concurrent editing | A stale edit form displays a conflict rather than overwriting a newer edit |
| Candidate queries | Name search, position and municipality filters, sort selection, per-page choice, next-page navigation and clear route produce scoped results |
| Logo/media | Valid PNG logo and JPEG logo replacement accepted; unsupported SVG rejected; versioned URLs refresh the displayed image |
| Candidate images | Valid JPEG/PNG upload/replacement displayed; fake PNG and 2 MiB + 1 byte rejected; municipal owned upload works; authorized candidate deletion cleans only owned uploaded media |
| Sensitive boundaries | No database entity/Identity table exposure; no passwords/hashes/connection strings/internal paths in UI; anonymous Admin media returns 401; raw image validation remains in the Phase 3 handler |
| Desktop/mobile | Real Chrome at 1440px desktop and 390x844 mobile; dashboard/list/form fit without whole-page horizontal overflow; image loading and zero browser page errors verified |
| Accessibility checks | Visible form controls have associated labels, screens have one main h1, login tab order and keyboard mobile menu toggling work |
| Aspire evidence | Dashboard opens with its private login token; service resources Running, structured Admin operation logs and Admin request traces visible |
| Phase 1 regression | Legitimate login then `/admin/connectivity` reaches API via discovery; unchanged Public WASM loads/reloads/calls gateway; health/OpenAPI and browser secret/dependency boundary pass |
| Phase 2 regression | Five tenants, 15 positions, 30 candidates, five accounts, seven roles; tenant EF guards, 20 SQL negative constraint checks, once-only seeds and rollback-only password/lockout checks pass |
| Phase 3 regression | Complete API CRUD, authorization/forgery/CSRF/concurrency/media/OpenAPI checks pass; real browser gateway authentication/tenant/media/logout checks pass |
| Restart persistence | Owned candidate contents/date/row version and owned administrator changed password/hash survive SQL container and Aspire restart; original seed records remain; only owned probe records removed |
| Existing data preservation | Full compared election/user/role/membership/seed-marker record fingerprint restored after owned CRUD/account cleanup |
| Assignment 1 | All 2,732 files match SHA-256, size and last-write baseline, including its SQLite database and Git files; nothing built/restored/run there |

Automated browser suite: nine grouped end-to-end checks in `artifacts/phase4/browser-verification.json`. Additional keyboard/Aspire assertions: `accessibility-observability.json`. Phase 3's existing fixture completion additionally verifies safe generic 500 behavior and the connected-session lifetime helper, without exposing a test HTTP authentication endpoint. Assertion scripts and the existing linked CLI checks keep the application project count at seven.

CRUD uses uniquely prefixed temporary records, never the seeded candidates. Password/lockout tests use a normal generated temporary administrator. Owned election records are cleaned in `finally`; fixture completion removes that account and compares records/password hashes. Existing data includes Municipalities, Positions, Candidates, AspNetUsers, AspNetRoles, AspNetUserRoles and SeedHistory. SQL identity and rowversion allocation counters can advance during owned/rollback-only tests; byte-identical database files are not claimed. Private account/cookie state stays in ignored `.local`, not screenshots or public result JSON.

## SQL/container preservation

Phase 3's original container-instance exception is acknowledged, not rewritten. The obsolete Phase 2 ID `90a767...` is not claimed preserved. Phase 4 began with current ID `e7fd02edfdaa499fb8c6ca179b91c3e680cd069787270457bdda608932ec7a26`, volume `mems-assignment2-sql-data`, database `MEMSAssignment2`. The existing container was explicitly started before Aspire. No SQL container configuration change, container/volume deletion, data reset, database recreation or import occurred. Initial and restart inspections verify this current instance and mount. Detailed final metadata is in `artifacts/phase4/sql-status.json`.

Both existing secret files are byte-hash compared with the Phase 4 starting baseline. Migrations, seed catalogs, bundled portraits/logos and Public WASM source are also checked unchanged. Session-revocation files persist separately under `.local/identity-sessions`; they do not alter Identity password hashes or seeded user rows. The separate revoked-cookie restart check records whether logout remains enforced after AppHost restart.

Final results confirm no unexpected container replacement during Phase 4, and a legitimately logged-out cookie remains rejected after the final same-build Aspire restart. The compared record fingerprint also exactly matches the original completed Phase 2 checkpoint. All temporary account/probe state and uploaded test files are cleaned up. Final process state: AppHost stopped; the current SQL container is exited, retained with its database and named volume.

## Evidence and commands

Screenshots under ignored `artifacts/phase4`: `super-dashboard.png`, `municipal-dashboard.png`, `municipality-details.png`, `candidate-details.png`, `restricted-delete.png`, `mobile-dashboard.png`, `mobile-candidates.png`, `mobile-candidate-form.png`, `aspire-dashboard.png`, `admin-structuredlogs.png` and `admin-traces.png`. These are real screenshots of the local application and test-owned content. Regression JSON/screenshots are copied under its `regression` directory. Database preservation, session restart, SQL identity, source integrity and final checks are recorded separately.

Key commands exercised:

```powershell
dotnet restore MEMS.sln --verbosity minimal
dotnet build MEMS.sln --no-restore --verbosity minimal
dotnet list MEMS.sln package --vulnerable --include-transitive --no-restore
.tools/dotnet-ef/dotnet-ef migrations has-pending-model-changes --project src/MEMS.Data --startup-project src/MEMS.Data --no-build

docker start mems-assignment2-sql
dotnet run --project src/MEMS.AppHost --no-build --launch-profile https -p:_AspireCliRunHookSuppressed=true
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode PreparePhase3
node scripts/verify-phase4.cjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode CompletePhase3
node scripts/verify-phase4-observability.cjs

powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode PreparePhase3
node scripts/verify-phase3.cjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode CompletePhase3
node scripts/verify-phase3-browser.cjs
node scripts/inspect-dashboard.cjs
node scripts/verify-phase1.cjs

powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode PrepareRestart
# Stop AppHost, restart the same SQL container, start AppHost again:
docker restart mems-assignment2-sql
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode CompleteRestart
node scripts/verify-phase4-session-restart.cjs prepare
# Restart AppHost using the same build/container.
node scripts/verify-phase4-session-restart.cjs verify
```

For ordinary use, run `docker start mems-assignment2-sql` followed by `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/start.ps1 -NoBuild`. Admin: `https://localhost:7252/admin`; login: `/admin/account/login`; protected connectivity: `/admin/connectivity`. API documentation: `https://localhost:7192/api/docs`; Public scaffold: `https://localhost:7290/mems-public/`; Aspire dashboard: `https://localhost:17048`, using the private login URL printed locally. Stop with Ctrl+C, then optionally `docker stop mems-assignment2-sql`.

## Issues resolved and limits

Initial compilation identified a Razor-reserved variable name, string-parameter bindings and static form initialization/nullability issues; these were corrected. Media replacements now include the current row version in image URLs so the browser refreshes them. Authentication review added shared session revocation and an explicit circuit expiry bound; tests cover logout/stamp rejection and expired/future session validation. The Aspire evidence script was corrected to commit its search filter and search root Admin request traces rather than only a child-span name. Final functional/build checks pass.

No Phase 4 functional blocker is identified. These are local integration/browser checks, not a full screen-reader audit, production penetration test or Azure deployment validation. The local session registry is suitable for this single-machine Aspire environment; multi-host deployment would need a shared session store, which is outside current scope. Tests do not change system time to expire real signed cookies; the lifetime comparison is covered directly while cookie issuance, stamp refresh and revocation use real sessions.

Before Phase 5: preserve the approved API/gateway route mapping, use canonical media paths correctly in WASM, retain all server tenant checks and keep any eventual voting choices entirely out of APIs/SQL. Phase 5 public pages and Phase 6 guide/printing remain unimplemented. Final architecture/ERD/deficiency/AI-use submission documents remain later work. AI assistance here was OpenAI Codex; no unperformed Copilot usage is claimed. No Git, GitHub, Azure, COMP4977, deployment, submission ZIP or final assignment-report actions occurred.
