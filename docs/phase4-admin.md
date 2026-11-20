# Phase 4 administration reference

The shared Blazor Server administration application is at `https://localhost:7252/admin`. It reuses the existing SQL Server/Identity store and the Phase 3 contracts and authorized election services. Public WASM remains the temporary connectivity page; final public election pages, voting selections, Session Storage and printing are not part of this phase.

## Start and stop

From `C:\Users\Asma\Documents\MEMS-Assignment-2`, with Docker Desktop running:

```powershell
docker start mems-assignment2-sql
dotnet restore MEMS.sln --verbosity minimal
dotnet build MEMS.sln --no-restore --verbosity minimal
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/start.ps1 -NoBuild
```

Start the existing container before Aspire. The Phase 3 container exception remains part of the project history; the obsolete original container ID is not claimed preserved. Phase 4 checks the current `e7fd02edfdaa499fb8c6ca179b91c3e680cd069787270457bdda608932ec7a26` instance and existing `mems-assignment2-sql-data` volume. Never reset/drop the database, remove/prune the volume/container or clear seed markers. No SQL container configuration change was required for Admin.

Stop AppHost with Ctrl+C. Optionally stop SQL with `docker stop mems-assignment2-sql`; it retains the container and volume. Before another build, stop AppHost to release assembly locks. Keep `.local/apphost-secrets.json`, `.local/identity-seed.json`, the protected Identity keys and local session-revocation files with this environment. No production bootstrap credential is introduced. The existing demo accounts are listed in `phase2-seeds.md`; obtain their current credentials locally, never from browser configuration or this document.

| Application | URL |
| --- | --- |
| Admin dashboard/login entry | `https://localhost:7252/admin` |
| Login | `https://localhost:7252/admin/account/login` |
| Public scaffold | `https://localhost:7290/mems-public/` |
| API documentation | `https://localhost:7192/api/docs` |
| OpenAPI | `https://localhost:7192/openapi/v1.json` |
| Aspire dashboard | `https://localhost:17048` (use the private login URL printed by AppHost) |

## Screens and routes

| Route | Behavior |
| --- | --- |
| `/admin` | Database-backed totals; system-wide for Super Admin, assigned tenant only for municipality admins |
| `/admin/account/login` | Static server-rendered Blazor form; real Identity credential check, lockout, safe local return URL and CSRF |
| `/admin/account/logout` | GET confirmation; antiforgery-protected POST ends the session |
| `/admin/account/change-password` | Current password, new password/confirmation and Identity validation; refreshes sign-in after success |
| `/admin/access-denied` | Safe permission explanation and navigation |
| `/admin/municipalities` | All permitted municipality records; municipality admins see only their own |
| `/admin/municipalities/create` | Super Admin tenant creation; normalized slug, election details and branding |
| `/admin/municipalities/{id}` | Details and navigation to scoped election records; Super Admin logo upload/replacement/removal |
| `/admin/municipalities/{id}/edit` | Super Admin validated edit and logo management |
| `/admin/municipalities/{id}/delete` | Super Admin destructive-action confirmation; dependencies prevent deletion |
| `/admin/positions` | Positions in the first permitted municipality, with authorized tenant selector |
| `/admin/{slug}/positions` | Tenant position list and seat counts |
| `/admin/{slug}/positions/create` | Create position in that authorized tenant |
| `/admin/{slug}/positions/{id}` | Position details and candidate navigation |
| `/admin/{slug}/positions/{id}/edit` | Edit name/available seats; tenant ownership remains fixed |
| `/admin/{slug}/positions/{id}/delete` | Confirmation; dependent candidates prevent deletion |
| `/admin/candidates` | Candidates in the first permitted municipality, with authorized tenant selector |
| `/admin/{slug}/candidates` | Name search, position filter, name/newest sorting, pagination, portraits and management links |
| `/admin/{slug}/candidates/create` | Create with a position from that tenant, biography and optional contact fields |
| `/admin/{slug}/candidates/{id}` | Biography, contact details, creation date, photo management and edit/delete links |
| `/admin/{slug}/candidates/{id}/edit` | Validated profile/position/contact edit and separate image management |
| `/admin/{slug}/candidates/{id}/delete` | Explicit confirmation showing the candidate and municipality |
| `/admin/media/{slug}/candidates/{id}/photo` | Authenticated media proxy to the Aspire-discovered API; current Identity cookie forwarded only to this internal service |
| `/admin/media/{slug}/municipalities/{id}/logo` | Equivalent authenticated logo route |
| `/admin/connectivity` | Protected interactive API/SQL connectivity check retained from Phase 1 |
| `/admin/error`, `/not-found` | Safe error/missing-page messages without exception or database details |

Municipality admins cannot create/edit/delete tenants or change their logos. Super Admin manages tenants across the system. Position/candidate tenant selection is limited to the actor's allowed directory. Existing records cannot be moved between municipalities by editing their parent tenant. A forged selection, URL slug or direct ID is independently rejected by the service, EF queries and database relationships. New municipalities use the same application; no project/resource is generated.

## Authentication and circuit security

Account pages use static server rendering with `ExcludeFromInteractiveRouting`; normal HTTP responses can issue/clear secure Identity cookies. Blazor EditForm supplies antiforgery tokens for their POSTs. The existing cookie name, key ring, SQL user/role store, password hashing, five-failure/fifteen-minute lockout and password policy are retained. Login POSTs are also rate limited. Return URLs are restricted to local Admin paths. Failed password input is cleared rather than echoed in returned HTML. There is no registration, password reset bypass, fake principal or test login endpoint.

Interactive management uses `AuthorizeRouteView`, fresh permission policies and a revalidating authentication-state provider. Every service operation creates a short-lived DI scope, rechecks the current stored account/stamp/role/tenant and uses the existing context factory. Components never retain an EF DbContext or UserManager for a circuit. Display state revalidates every 30 seconds; mutations do not wait for that timer to enforce permission changes.

A random server-issued session claim and expiry are protected inside the existing Identity cookie. Logout records that session's revocation under ignored `.local/identity-sessions`; Admin and API share that local registry. Existing circuits check revocation and expiry on their next operation, and replayed cookies fail validation. Account switching revokes the preceding browser session. Password changes use Identity's security-stamp change and refreshed cookie. This prevents stale circuits without modifying seeded users' stored stamps/passwords during ordinary logout. Session lifetime is bounded by the issuing cookie's expiry; a connected circuit cannot extend it indefinitely. The registry is a local, single-machine Aspire design, not a distributed production session store. It contains opaque session IDs/timestamps, no passwords or ballot selections. Keep it across restarts so logged-out sessions stay revoked.

UI visibility is a convenience; server policies, `ElectionAccess`, `ElectionApiService`, EF tenant filters/write guards and existing SQL constraints remain authoritative. Municipality operations recheck Super Admin; candidate writes validate the submitted position inside the resolved tenant. ETags/row versions reject stale edits instead of silently overwriting changes. Parent delete restrictions produce safe feedback and preserve data.

## Media, validation and usability

The shared Data media handler enforces the Phase 3 JPEG/PNG decode, actual format, 2 MiB, dimension, generated filename and sanitized PNG rules. Blazor additionally bounds the browser stream. Files are stored in the same tenant-owned local media directory used by the API. Admin images are served through an authorized API proxy rather than copying seed portraits into a new static directory. Replacement URLs include the new row version so displayed images refresh. Seed image files are not overwritten or deleted by cleanup.

Shared request DTO validation is reused by Blazor EditForm/DataAnnotationsValidator, services and API; SQL constraints remain unchanged. Forms provide summaries and field messages, while business/authorization/concurrency failures have safe alerts. Name/biography/contact/seat limits are retained from the preceding phases. Text is rendered with normal Razor HTML encoding. No connection string, hash, key, internal asset path or credential is placed in the interface.

Bootstrap-based styling provides responsive navigation, dashboard cards, tables, filters, labeled forms, keyboard focus, a skip link, loading/empty/error states and success feedback. Tables may scroll inside their panels on narrow screens; the page itself fits a 390px viewport. Delete routes require an explicit confirmation action. Image removal requires its confirmation checkbox. Desktop/mobile screenshots and browser assertions are recorded in `artifacts/phase4`.

## Repeat verification

Start the stack and run the existing guarded fixture workflow. It creates a normal municipality administrator only for password/lockout tests. All election CRUD uses uniquely named owned records, never the seeded candidates.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode PreparePhase3
node scripts/verify-phase4.cjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode CompletePhase3
node scripts/verify-phase4-observability.cjs
```

The mode names retain their Phase 3 names because the same guarded fixture implementation is reused, not duplicated. Completion removes only the owned account and compares the existing election/Identity record fingerprint; its result can be copied into Phase 4 evidence. Private fixture state is in ignored `.local/phase3-fixtures.json` until completion. Browser CRUD cleanup is in `finally`, guarded by its unique prefix and recorded in `.local/phase4-created-records.json`. Do not delete unrelated rows to resolve failures. An interrupted run must finish owned-record/account cleanup before another preparation. No eighth test project is needed.

For regressions, after fixture completion:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1
node scripts/inspect-dashboard.cjs
node scripts/verify-phase1.cjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode PreparePhase3
node scripts/verify-phase3.cjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode CompletePhase3
node scripts/verify-phase3-browser.cjs
```

Phase 1's test now signs in legitimately and visits `/admin/connectivity`; the Public scaffold and original API/SQL readiness/privacy checks are retained. Phase 2 verifies rollback-only SQL constraints and seed behavior. Phase 3 checks cookie/CSRF/API isolation and media independently of Admin UI. Restart persistence uses the existing PrepareRestart/CompleteRestart workflow described in `phase2-verification.md`. All commands target this local Assignment 2 environment only.

Detailed observed results and limitations are in `phase4-verification.md`; exact source changes are in `phase4-files.txt`. AI assistance was OpenAI Codex; retained Copilot instruction files do not imply unperformed GitHub Copilot usage. No Git, Azure, COMP4977 or deployment actions are part of these instructions.
