# Technical guide and reproducibility

## Local runtime

Verified platform: Windows, .NET SDK 10.0.402 / target .NET 10, installed Aspire CLI 13.6.4 and Docker Desktop Linux Engine. SQL image and all package versions remain pinned in the existing project files. Browser tests use the existing workspace Playwright installation and installed Chrome; PDF inspection uses the existing pypdf tool. No Azure deployment is required.

The solution's seven projects are AppHost (orchestration), ServiceDefaults (server health/discovery/observability), Admin (Interactive Server UI), Public (standalone WASM), Api (REST), Data (EF/Identity/services/media/migrations) and Shared (safe contracts). See the rendered architecture/ERDs and generated SQL dictionary. Test C# files are compiled into the existing API under guarded Development CLI modes; they do not form an eighth project.

## Start this existing workspace

Preserve the current container/volume and ignored `.local` secrets, Identity keys and session registry. Start Docker Desktop if needed, then from the solution root:

```powershell
dotnet restore MEMS.sln
dotnet build MEMS.sln --no-restore
docker start mems-assignment2-sql
aspire run --apphost src/MEMS.AppHost/MEMS.AppHost.csproj --no-build --launch-profile https --non-interactive --nologo
```

`scripts/start.ps1 -NoBuild` invokes that Aspire command. Use the already trusted development certificate; no certificate installation/trust modification was needed in verification. Do not use isolated mode or change the SQL resource configuration on this retained environment. Stop AppHost using Ctrl+C and optionally `docker stop mems-assignment2-sql`; never remove/prune the container or volume.

| Service | Local URL |
| --- | --- |
| Public discovery | https://localhost:7290/ |
| Tenants | /burnaby, /surrey, /richmond, /pitt-meadows, /maple-ridge on the Public origin |
| Candidate/position directories | /{slug}/candidates and /{slug}/positions; append /{id} for details |
| Personal guide | /{slug}/guide |
| Shared Admin | https://localhost:7252/admin |
| API documentation | https://localhost:7192/api/docs |
| OpenAPI | https://localhost:7192/openapi/v1.json |
| Aspire dashboard | https://localhost:17048 (private login link printed locally) |

## New-machine source handoff

The planned source package excludes `.local`, `.tools`, Docker data, databases, uploads, keys and credentials. On a genuinely separate clean environment, install the prerequisites, restore/build the solution, run `scripts/configure-development-seed.ps1`, enter the course development credential through its secure prompt, and launch `aspire run` as above. AppHost creates a random ignored local SQL secret once; API applies the two existing reviewed migrations and completed-version seed batches to its dedicated Development database. No password is included in source, report, browser assets or command-line examples.

Do not start a second fresh copy against this user's existing fixed container name: the existing SQL secret must remain paired with its volume. A clean-machine first-run test has not been performed; the existing environment's restore/build/startup and schema reproduction via migrations are verified. Assignment 1 baselines and local verification fixtures are environment-owned and are not included in the proposed handoff.

## Data, authentication and tenant security

SQL contains five tenants, 15 positions, 30 fictional candidates, five existing administrator accounts and seven roles. Accounts include `super`, `pm`, `Burnabyadmin`, `Surreyadmin` and `Richmondadmin`; credentials are obtained from the course/local ignored configuration, not this report. Unique slugs, scoped position names, composite candidate/position ownership and restrictive deletes enforce relationships. One-time seed history preserves sample edits/deletions and existing password changes; startup does not reset records.

Admin/API reuse ASP.NET Identity storage and protected cookie/session mechanisms. Super Admin has authorized system access; a municipal account has exactly one canonical tenant assignment. UI hiding is supplemented by service/policy checks, scoped EF filters, original/current ownership validation and SQL constraints. Admin operations create short-lived DI scopes, avoiding a circuit-long EF context. Cookies are secure/HttpOnly, modifying HTTP requests require CSRF, security stamps and logout revocation are checked, and existing lockout behavior remains. Test authentication uses legitimate sessions, not fake headers or a bypass.

The API exposes Shared DTOs, validation and safe Problem Details rather than EF/Identity entities. Optimistic versions/If-Match protect writes. JPEG/PNG uploads are limited to 2 MB and validated through actual decoding/re-encoding with SkiaSharp; tenant authorization and generated storage paths protect images. Seeded portraits and trusted avatars are preserved. Local upload files are separate from configuration/keys and served through ownership-scoped endpoints.

## Election privacy and printing

The page 6 Local Storage wording conflicts with the explicit Session Storage privacy requirements on pages 7–8 and the marking guide. Native sessionStorage and WASM JS interop implement the latter. Candidate IDs are stored under municipality-specific keys; add/remove/clear makes no backend request and changes no URL. The guide fetches the same active-tenant catalog regardless of selections and resolves saved IDs locally, so even record lookup patterns do not disclose a selection list. No ballot table, vote API, running tally or administrator choice screen exists.

Normal same-tab navigation/refresh persists choices. Independent tabs start empty; opener/duplicate/session restore behavior follows the browser and can copy/restore session state. Clear guide removes the active tenant's key. Storage errors have no server/persistent fallback. Print opens native browser preview, hides controls and includes municipality, election date, candidates, positions and local print date; intentional prints/PDFs exist outside session storage. No arbitrary count cap is imposed because the PDFs do not explicitly require one.

## Verification and operational limits

The final verification matrix records fresh Phase 7 runs of the existing scripts; engineering histories remain under `docs/phase1` through `docs/phase6`. Owned CRUD accounts/records are cleaned and complete record/password fingerprints checked. Read-only SQL metadata exports contain no Identity row values. SQL allocation counters may advance during owned/rollback tests; byte-identical database files are not claimed. The historical Phase 3 container-instance replacement retained its volume and data; Phase 7 verifies the current ID without claiming that older instance survived.

Physical printer output, Safari/Firefox, real mobile hardware, full screen-reader/WCAG audit, clean-machine installation and public deployment were not performed. The current local session registry/Windows DPAPI design was verified for this single-machine environment, not a multi-host production deployment. No COMP4977/JWT integration requirement was assumed. These limits and outstanding feedback/Copilot/publication items are explicit in the readiness checklist.
