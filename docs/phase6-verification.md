# Phase 6 verification — personal and printable voting guide

Completed November 16, 2026 (Pacific) in `C:\Users\Asma\Documents\MEMS-Assignment-2`. Phase 6 only: no Git, commits, pushes, Azure, COMP4977, deployment, submission packaging or Phase 7 work. Assignment 1 remains strictly read-only. This is an engineering checkpoint, not the final assignment submission report.

## Delivered implementation and inventory

Anonymous visitors can add/remove candidates from existing directories, overviews, position pages and profiles; duplicate IDs are prevented and button/feedback state stays synchronized within the tab. Every municipality has a reusable `/{slug}/guide` with current candidate/position names, individual removal, clear confirmation/cancel, empty/error states and Print voting guide. The Bootstrap interface supports keyboard controls, visible focus, accessible button names/pressed states and live feedback. Existing public and Admin pages remain operational.

Choices are stored only in native browser Session Storage through a WASM JS module. Keys use municipality IDs, so tenant routes cannot mix choices. Invalid storage/schema and missing/deleted/foreign IDs are handled safely. Blocked/quota/write failures show meaningful errors without falling back to persistent or server storage. No selection cap is imposed: neither assignment PDF explicitly requires one. Seat counts remain informational. The user’s current instruction and actual rubric take precedence over the extra limit in the old Assignment 1 implementation/starter proposal.

Printing includes all five required fields: municipality, election date, selected candidate names, each position and print date. `window.print()` opens normal browser preview; `beforeprint` refreshes the local date for button/browser printing. Print CSS hides site controls/navigation, uses readable typography and Letter/A4-compatible margins, repeats table headers and avoids splitting rows. Empty guides print an explicit empty message. Required election/name metadata must be present before a printable document is shown.

The complete created/modified source/documentation inventory is [phase6-files.txt](phase6-files.txt). Seven `.csproj` projects remain; no eighth project, package, model, migration, API or table was added. AppHost, Admin, API, Data, Shared and ServiceDefaults source/configuration are unchanged: **120 protected files** match the Phase 6 starting hashes, including SQL orchestration, migrations, Identity/seeds and packaged images. The startup script now invokes the installed Aspire CLI. Private runtime/build/browser/fixture outputs are ignored and identified separately in the inventory.

Design/usage, exact routes, API neutrality, printing and source references: [phase6-guide.md](phase6-guide.md).

## Privacy and rubric decision

Assignment 2 page 6 says Local Storage. Pages 7–8 explicitly require Session Storage, prohibit transmitting choices to APIs/SQL and require session-end removal; page 16 assigns five marks to Election Privacy & Session Storage Implementation. The implementation follows those specific requirements and the explicit Phase 6 authorization. It uses native `sessionStorage`, not Local Storage, cookies, SQL, API storage or server sessions. Assignment 1's server-session implementation was not imported.

The stored value contains only version, municipality ID and unique candidate IDs. Add/remove/clear never invoke PublicApi or change URLs. The guide fetches the **same complete active-tenant public catalog**, page by page with fixed query options, whether empty or populated; saved IDs are resolved locally. It makes no lookup/photo request based on which candidates are selected. There is no API operation for recording selections or computing tallies, no SQL choice storage and no Admin selection interface. WASM browser telemetry remains disabled. The existing server logs therefore receive ordinary public/catalog requests, without selection lists/actions/payloads.

Normal Session Storage survives refresh/navigation, is separated between independent tabs and ends when the tab normally closes. Opener/duplicated/session-restored tabs can copy or restore browser session state; this normal browser behavior is documented in the UI and [MDN](https://developer.mozilla.org/en-US/docs/Web/API/Window/sessionStorage). Explicit Clear guide is available on shared devices. There is no unload handler that would erase choices during refresh. Intentional printing/PDF saving creates a user-controlled copy outside the session lifecycle.

## Actual verification

Evidence is retained locally in ignored `artifacts/phase6/`, with prior-phase regression results copied into `regression/`. No unperformed test is marked passed.

| Verification | Result and evidence |
| --- | --- |
| Restore/seven-project build | Passed; final build **0 warnings, 0 errors**. `restore.txt`, `build.txt`. |
| Dependencies/model | Current NuGet advisory audit reported no known vulnerable packages in all seven projects, including transitives; no package versions changed. EF CLI reports no pending model changes. `package-audit.txt`, `model-check.txt`. |
| Professor-required startup | Actual `aspire run --apphost src/MEMS.AppHost/MEMS.AppHost.csproj --no-build --launch-profile https --non-interactive --nologo` succeeded with installed CLI **13.6.4** and the existing trusted certificate. No certificate installation/trust change or infrastructure configuration change was needed. |
| Aspire connectivity | Dashboard/resources opened; Admin, API and gateway health all returned 200 after restart. SQL connected through the existing Aspire reference; logs/traces and same-origin public requests passed the infrastructure regression. |
| Main Phase 6 browser suite | **11 groups passed**, **133 anonymous GET API requests**, zero browser page errors, zero application database mutations. `browser-verification.json`. |
| Selection operations | Anonymous keyboard add/remove, duplicate prevention across directory/profile, rapid concurrent card changes, guide removal, clear confirm/cancel and navigation/refresh persistence passed. Two Mayor candidates can be selected even with one available seat. |
| Municipality isolation | Burnaby/Surrey/Richmond choices remain separate; Pitt Meadows/Maple Ridge guides and mixed-case Richmond work. Unknown tenants fail safely. Clearing Burnaby does not clear Surrey. Forged foreign IDs are removed locally without a foreign-ID API request. |
| Network privacy | Add/remove/clear/print operations made **zero API requests** and did not change the URL. Empty and populated guide reloads had **identical** catalog/media request lists. Observed public API traffic had no body, cookies or Authorization header. No Local Storage entries or public choice cookies were created. |
| Session lifecycle | Same-tab refresh/navigation retained choices. Independent tabs began empty; an opener-created tab received the normal initial copy. Closing the selected tab and opening a fresh independent tab yielded an empty guide. A live tab also retained its choice across actual SQL/Aspire restart. `session-restart.json`. |
| Storage failure/stale IDs | Corrupt JSON/schema recovery, duplicate ID normalization, nonexistent/foreign candidate cleanup, blocked storage and quota/write errors passed without a persistent/server fallback or a false “saved” state. |
| Live Admin data synchronization | One owned candidate was created through legitimate authenticated API CRUD, selected anonymously, edited to a **100-character name** and different own-tenant position, and deleted. Guide refresh reflected edits and pruned the deleted choice; storage key was removed. Admin origin had no public choice storage. Owned record cleaned. `admin-sync.json`. |
| Missing fields | A controlled public-response fixture with missing election metadata showed a safe unavailable state and no Print control instead of a document with missing fields. Actual populated PDFs include all five required fields. |
| Letter/A4 PDF validation | **Six PDFs inspected** using pypdf: populated and empty guides each one page on both paper sizes; 32 long-name rows each produced **five pages** on Letter/A4. All required fields/paper dimensions passed; controls were absent, and every complete long name remained on a single page. `print-verification.json`. |
| Native print preview | The real Print button opened **native headed Chrome preview**. Both Letter and A4 were selected and captured; preview was cancelled without clicking Print or sending a physical job. `native-print-preview.json` and screenshots. |
| Desktop/mobile/accessibility | 1440px desktop and 390×844 mobile guide layouts fit without horizontal overflow, including long-name fixtures. Labels, pressed state, live feedback, keyboard add/remove and skip-link focus passed; screenshots visually reviewed. |
| Browser secret/dependency boundary | **434 built/source public asset files** scanned for known SQL, seed-password and current dashboard secret values; none found. Public remains Shared-only with no Data/EF/SQL/server ServiceDefaults dependency. `integrity.json`. |
| Phase 1 regression | **5 groups** passed: SQL-backed health, Admin interactivity, WASM/reload/same-origin API/config privacy and dashboard logs/traces. |
| Phase 2 regression | Required 5 tenants/15 positions/30 candidates/5 users/7 roles; EF tenant scoping, Identity, **20 SQL negative constraints**, restrictive relationships, reseed/lockout and idempotence passed. Rollback-only mutations restored the record fingerprint. |
| Phase 3 regression | **9 API groups + 4 browser-security groups** passed: CRUD, parent/tenant ownership, forged IDs, 401/403, CSRF/origin, validation, optimistic concurrency, image validation, safe DTOs/docs, real cookie login/logout and lockout. |
| Phase 4 regression | **9 browser groups + 4 accessibility/observability groups** passed: all five original logins, dashboards/CRUD/media, tenant boundaries, owned-account password/lockout, stale edits and circuit/logout protection. Revoked cookie remained rejected after restart. |
| Phase 5 regression | **9 browser groups + 5 navigation groups** passed: all tenants, 24 JPEGs/6 avatars/logos, API search/filter/sort/paging, details/refresh/history, graceful errors, fresh browsing with no initialized choices, Admin sync, media and delayed-tenant navigation. Its untouched browsing contexts correctly retain zero stored choices. |
| Actual SQL persistence | Same SQL container and Aspire restarted. Owned candidate fields/date/row version, changed test password/hash and complete probe fingerprint persisted. Owned probes removed; original fingerprint restored. |
| Existing data/passwords | Final record fingerprint matches the original completed Phase 2 checkpoint for Municipalities, Positions, Candidates, AspNetUsers, AspNetRoles, AspNetUserRoles and SeedHistory. No original account reset or seed overwrite. Fixture states are completed; owned uploaded files cleaned. |
| Schema/migrations | Read-only SQL inspection found exactly **12 original tables**, the **two original migrations**, no ballot/vote/selection columns and baseline counts **5/15/30/5/7**. `schema-verification.json`. |
| Assignment 1 | All **2,732 files** match original SHA-256, size and last-write-time baseline, including SQLite and Git files. No application commands or writes ran there. `assignment1-integrity.txt`. |

Browser-only response fixtures test extreme long names, catalog paging and unavailable metadata; they are not records seeded into SQL. The separate Admin fixture driver deliberately writes one owned candidate solely for edit/deletion synchronization testing; those writes are administrative CRUD, not public selection requests. Original seeded candidates are not repurposed for live CRUD.

The initial PDF test accidentally kept Playwright's screen-media override active for later empty-guide exports. Resetting that test override restored correct print-media exports; final six-PDF inspection passes. A concurrent-input test initially raced Playwright's shared mouse pointer; dispatching the intended distinct DOM button events verified serialized state changes correctly. Chrome preview needed its own workspace test profile/context and DevTools adapter because internal preview targets are outside an incognito page list. These harness fixes did not weaken application checks. Storage handling was tightened so failed reads/writes cannot silently claim success.

## Database/container preservation

Existing database `MEMSAssignment2`, container `mems-assignment2-sql` and volume `mems-assignment2-sql-data` (mount `/var/opt/mssql`) were retained. Phase 6 started and finished with current container ID:

```text
e7fd02edfdaa499fb8c6ca179b91c3e680cd069787270457bdda608932ec7a26
```

The ID/mount were verified after CLI startup and actual restart. **No Phase 6 container replacement occurred.** The original Phase 2 `90a767...` instance was replaced during Phase 3 as documented earlier; its preservation is not claimed. No container/volume removal, recreation, pruning, database drop/reset, imports or SQL resource changes occurred. Both existing development secret files match their Phase 6 starting hashes.

The unchanged migrations remain:

- `20261116003601_InitialSqlServerIdentityTenancy`
- `20261116004505_FixHyphenatedSlugsAndAssetNames`

Tests compare original records/hashes, not byte-identical SQL files: owned writes and rollback tests may advance identity and rowversion allocation counters. Native browser test profiles, session revocations, build outputs and evidence are ignored local artifacts; no choice storage was added to the application server. Final services are stopped with the database/container/volume retained; `sql-status.json` records the final state.

## Evidence and exact commands

Screenshots: [desktop guide](../artifacts/phase6/burnaby-guide-desktop.png), [mobile guide](../artifacts/phase6/burnaby-guide-mobile.png), [Surrey guide](../artifacts/phase6/surrey-guide.png), [print layout](../artifacts/phase6/burnaby-print-layout.png), [native Letter preview](../artifacts/phase6/native-print-preview-Letter.png), [native A4 preview](../artifacts/phase6/native-print-preview-A4.png). PDF evidence includes `burnaby-guide-Letter.pdf`, `burnaby-guide-A4.pdf`, both empty guides, both five-page long guides and a live edited-candidate guide. These are local verification artifacts, not submission packaging.

From `C:\Users\Asma\Documents\MEMS-Assignment-2`:

```powershell
docker start mems-assignment2-sql
aspire run --apphost src/MEMS.AppHost/MEMS.AppHost.csproj --no-build --launch-profile https --non-interactive --nologo
```

Equivalent startup: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/start.ps1 -NoBuild`. When rebuilding, stop Aspire first, then `dotnet restore MEMS.sln --verbosity minimal` and `dotnet build MEMS.sln --no-restore --verbosity minimal`. The application uses the already installed CLI/trusted certificate; no install/trust step is needed. Do not use `--isolated` with this retained database. Stop with Ctrl+C; optionally stop only this SQL container with `docker stop mems-assignment2-sql`.

Public: `https://localhost:7290/`; guides at `/burnaby/guide`, `/surrey/guide`, `/richmond/guide`, `/pitt-meadows/guide`, `/maple-ridge/guide`. Admin: `https://localhost:7252/admin`; API docs: `https://localhost:7192/api/docs`; Aspire dashboard: `https://localhost:17048` using its private terminal login link, never published.

Principal verification commands exercised:

```powershell
node scripts/verify-phase6.cjs
python -B scripts/verify-phase6-print.py   # Used the existing installed Python interpreter
node scripts/verify-phase6-sync.cjs
node scripts/verify-phase6-preview.cjs   # Visible preview; cancels, never prints
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/inspect-phase6-schema.ps1
```

Preservation/regression workflows reused `scripts/verify-phase2.ps1` Verify/PreparePhase3/CompletePhase3 and PrepareRestart/CompleteRestart modes, plus `verify-phase1.cjs`, `verify-phase3.cjs`, `verify-phase3-browser.cjs`, `verify-phase4.cjs`, `verify-phase4-observability.cjs`, `verify-phase4-session-restart.cjs`, `verify-phase5.cjs` and `verify-phase5-navigation.cjs`. Baseline-dependent tests ran after owned record cleanup. `verify-phase6-session-restart.cjs` holds a live browser tab and uses private ready/resume handshake files around the actual SQL/Aspire restart. Keep incomplete fixture state until its guarded cleanup completes; never overwrite it or reset data to recover a test.

## Limits and unresolved requirements

No Phase 6 functional blocker remains. Physical printer output, Safari/Firefox, real mobile hardware and a full screen-reader/WCAG audit were not performed; evidence covers installed Chrome, mobile viewport simulation, native preview, PDF inspection and the exercised accessibility controls. Browser/session restore behavior is governed by the browser and documented above; Session Storage is not a guarantee that a restored tab loses its state.

The Local Storage/Session Storage wording conflict remains an instructor-document contradiction; the explicit privacy section, marking guide and current user instruction determine the implemented Session Storage choice. No instructor clarification about COMP4977 consumption was assumed. Phase 7/final assignment architecture/ERD/report/repository/submission delivery remains outside this authorization. The rubric's GitHub Copilot usage is not falsely claimed: this work used OpenAI Codex, with the actual prompt/instructions recorded. No submission PDF/Word report, source ZIP, Git operation or deployment was produced. Stop after Phase 6.
