# Phase 6 guide usage and privacy design

Visit the existing public website anonymously, choose a municipality and select **Add to guide** on a candidate card or profile. The button changes to **Remove from guide**, and its pressed state and live feedback identify the current choice. The municipality navigation links to **My voting guide**. On that page, remove candidates individually or choose Clear guide and confirm; cancelling keeps the choices. Selection changes never require login.

Exact local guide URLs:

- `https://localhost:7290/burnaby/guide`
- `https://localhost:7290/surrey/guide`
- `https://localhost:7290/richmond/guide`
- `https://localhost:7290/pitt-meadows/guide`
- `https://localhost:7290/maple-ridge/guide`

The same reusable route supports future municipalities; canonical/mixed-case slug handling is unchanged. A new tenant does not require a new app. Seat counts are displayed for information. Assignment 1's old controller imposed a seat limit, but neither assignment PDF expressly requires a selection cap; the current Phase 6 authorization explicitly forbids arbitrary limits. The new guide imposes none.

## Storage decision

Assignment 2 page 6 mentions Browser Local Storage, while pages 7–8 explicitly require Browser Session Storage, forbid sending choices to APIs/SQL, and require session-end removal. Page 16 also allocates five marks to Election Privacy & Session Storage Implementation. Phase 6 follows the more specific privacy requirements and the user's explicit instruction: **native `sessionStorage` through WASM JavaScript interop**. No Local Storage package or new NuGet dependency was added. Assignment 1's server-session choice storage was not reused.

`MEMS.Public/Services/VotingGuideStore.cs` is scoped within standalone WASM, so its in-memory state and JS calls run in the browser, not Admin or API. A small imported JS module reads/writes only `sessionStorage`. Keys use the stable municipality ID (`mems.guide.v1.{municipalityId}`); values contain a version, municipality ID and unique positive candidate IDs. No account identifier, name, biography, contact information or choice count is sent to a server. A municipality slug rename retains the same ID; a different municipality reusing a slug cannot inherit its choices. Clear removes only the current tenant's key, not unrelated storage.

Selections survive ordinary navigation and refresh in the same tab. Fresh independent tabs begin empty. Opener-created tabs receive the browser's normal initial copy, and duplicate/session-restored tabs may retain or restore session data. No unload handler clears choices on ordinary navigation. The UI explains this behavior and provides explicit clearing. These lifecycle details follow [MDN's sessionStorage documentation](https://developer.mozilla.org/en-US/docs/Web/API/Window/sessionStorage). Storage denial/quota/write errors show a safe message; there is no fallback to cookies, Local Storage, a server session or a database.

## Keeping choices off the network

Add, remove and clear only call the browser store. They do not invoke PublicApi or change the URL. Candidate IDs appearing in ordinary public profile navigation still identify the viewed public record; no URL contains a list of choices or an add/remove action. WASM telemetry remains disabled in the preserved gateway configuration.

The guide fetches the municipality, positions and **all pages of that municipality's public catalog**, with fixed API query options (`pageSize=100`, `sort=name`) independent of the choices. It then matches the saved IDs locally. Empty and populated guides issue identical catalog/media requests. It never retrieves individual candidates or photographs based on which candidates were selected. No POST/PUT/DELETE, selected-ID query, selection request body or choice-specific media lookup is needed. These remain bounded tenant queries through the existing API, not access to the full database.

The catalog is checked for tenant ownership and valid positions. Duplicate/foreign/nonexistent/deleted IDs are removed locally only after successful catalog loading; an unavailable API does not erase saved choices. Current API names/positions appear after refresh because only IDs are saved. Required print metadata must be present before a printable document is shown. Invalid JSON/schema is cleared for that key with a notice. The state-changing interop calls are serialized to prevent concurrent card clicks from losing selections.

No server source, entity, migration, API contract or administrative screen was changed for this feature. Existing SQL tables/records and OpenAPI remain the foundation from Phase 5. Admin runs on a different origin and has no access to the public origin's Session Storage; no admin interface or endpoint receives or displays voter choices. Existing tenant authorization remains authoritative for administrative access.

## Printing

Choose **Print voting guide** to invoke `window.print()` and the browser's normal print preview. The document includes municipality, election date, selected candidate names, each position and a browser-local print date/time. It is a personal checklist, not a vote submission. Empty guides print an explicit No candidates selected message with municipality/election/print dates.

`beforeprint` updates the date synchronously for both the button and the browser's own Print command. Print CSS hides site navigation, branding banner, buttons, filters, help/feedback and other controls, uses readable 11pt text and margins, repeats table headers and avoids splitting candidate rows. Paper size is automatic so Letter or A4 can be selected in the dialog. Disable browser headers/footers for the cleanest copy. Saving a PDF or printing intentionally creates a copy outside this tab's lifecycle.

The implementation uses [Microsoft's JS module interop](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/location-of-javascript?view=aspnetcore-10.0), native [beforeprint](https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeprint_event) and [print CSS](https://developer.mozilla.org/en-US/docs/Web/CSS/Guides/Media_queries/Printing). Print-date updates are browser DOM operations; no choices go back to .NET server services.

## Start and verify

From `C:\Users\Asma\Documents\MEMS-Assignment-2`, with Docker Desktop available:

```powershell
docker start mems-assignment2-sql
aspire run --apphost src/MEMS.AppHost/MEMS.AppHost.csproj --no-build --launch-profile https --non-interactive --nologo
```

The startup script also uses that Aspire CLI command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/start.ps1 -NoBuild
```

Stop with Ctrl+C, then optionally `docker stop mems-assignment2-sql`. Retain the existing container, volume, ignored secrets and Identity key/session files. Do not use isolated mode, remove/prune Docker storage or reset seed data. Restore/build only this Assignment 2 solution when needed.

With Aspire running and the existing workspace browser/PDF tools available:

```powershell
node scripts/verify-phase6.cjs
# Existing Python interpreter, with -B:
python -B scripts/verify-phase6-print.py
node scripts/verify-phase6-sync.cjs
node scripts/verify-phase6-preview.cjs
```

The sync script creates/edits/deletes one ownership-prefixed candidate through legitimate Admin API authentication; its fixture driver is separate from the choice-free public client. The preview script opens a temporary visible Chrome preview and cancels without clicking Print. Machine-readable results/screenshots/PDF evidence are in ignored `artifacts/phase6`; private state/test profiles remain ignored under `.local`. See `phase6-verification.md` for observed results and `phase6-files.txt` for the exact inventory. Phase 7, deployment, Git and submission packaging are not authorized by this checkpoint.
