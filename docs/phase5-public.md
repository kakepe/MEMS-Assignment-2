# Phase 5 public election information

MEMS.Public is one standalone .NET 10 Blazor WebAssembly application. It uses the existing MEMS.Api and MEMS.Shared DTOs through the same-origin Aspire gateway. It has no Data, EF Core, SQL client, server ServiceDefaults or Identity infrastructure reference. Admin remains the existing separate Blazor Server application.

## Run the local application

From `C:\Users\Asma\Documents\MEMS-Assignment-2`, with Docker Desktop running:

```powershell
docker start mems-assignment2-sql
dotnet restore MEMS.sln --verbosity minimal
dotnet build MEMS.sln --no-restore --verbosity minimal
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/start.ps1 -NoBuild
```

The already-built application only needs the Docker start and `start.ps1 -NoBuild` commands. Stop AppHost with Ctrl+C; stop SQL without removing it using `docker stop mems-assignment2-sql`. Stop AppHost before rebuilding. Keep the existing database volume and ignored development secrets/key/session files. Do not remove/prune containers or volumes, reset the database, clear seed markers or import Assignment 1 databases.

| Service | Local address |
| --- | --- |
| Public landing | `https://localhost:7290/` |
| Burnaby | `https://localhost:7290/burnaby` |
| Surrey | `https://localhost:7290/surrey` |
| Richmond | `https://localhost:7290/richmond` |
| Pitt Meadows | `https://localhost:7290/pitt-meadows` |
| Maple Ridge | `https://localhost:7290/maple-ridge` |
| Public service status | `https://localhost:7290/_status` |
| Admin | `https://localhost:7252/admin` |
| API reference | `https://localhost:7192/api/docs` |
| OpenAPI | `https://localhost:7192/openapi/v1.json` |
| Aspire dashboard | `https://localhost:17048` (use its private login URL printed locally) |

HTTP on port 5290 redirects browser document navigation to HTTPS. Use the HTTPS addresses for normal verification.

## Public routes

| Route | Content |
| --- | --- |
| `/` | Dynamic municipality discovery, logos, province and election date |
| `/{slug}` | Tenant branding, election date/information, positions, candidate totals and a small candidate preview |
| `/{slug}/candidates` | Tenant candidate directory with portraits, name search, position filter, stable sort and pagination |
| `/{slug}/candidates/{id}` | Scoped candidate biography, photograph, position and optional public contact information |
| `/{slug}/positions` | Scoped position directory and available seats |
| `/{slug}/positions/{id}` | Position details, seat count and paged candidates for that position |
| `/_status` | Anonymous connectivity check retained for infrastructure regression |
| `/_not-found` | Safe missing-page screen; the same screen handles unmatched client routes |

Slugs are normalized to lowercase before API requests, so `/Richmond` and `/RICHMOND` display Richmond. Generated links use the canonical lowercase slug. Invalid/unknown slugs and foreign IDs produce safe Not Found content. A new municipality created in Admin appears through discovery after normal refresh; no public project/build is created per tenant. Utility routes use `_` prefixes, which cannot collide with valid municipality slugs.

File-like invalid tenant document URLs (for example `/bad.slug`) redirect to `/_not-found`; missing script/asset requests retain HTTP 404. Malformed, overflowing or invalid numeric filter/page parameters produce safe 400 content rather than a Blazor parameter-binding failure. The candidate directory offers Reset filters to recover.

The old `/mems-public/` entry redirects to `/`. Legacy deep links such as `/mems-public/burnaby` redirect to `/burnaby`. The API proxy and browser configuration retain their existing transport paths for compatibility. Direct tenant/profile/position URLs and refresh are served by the SPA fallback, while browser back/forward state comes from route/query parameters.

## Gateway and API integration

The public HTML base is `/`. The existing managed `mems-public-gateway` runtime is adapted from the pinned Aspire gateway source in `src/MEMS.AppHost/Gateway/PublicGateway.cs`, retaining its MIT attribution, service discovery, configuration and observability. Static assets/SPA fallback are mapped at the root; API/config routes retain the prior prefix. This is the same existing gateway runtime resource, excluded from AppHost compilation as an entry point; no eighth solution/application `.csproj` was added. AppHost still uses the pinned Aspire Blazor integration. No SQL resource configuration change was needed.

Browser API transport: `/mems-public/api/mems-api/api/...`. It forwards to the actual Phase 3 API via Aspire service discovery. Examples:

```text
GET /mems-public/api/mems-api/api/municipalities
GET /mems-public/api/mems-api/api/burnaby/municipalities
GET /mems-public/api/mems-api/api/burnaby/positions
GET /mems-public/api/mems-api/api/burnaby/candidates?search=Vivian&positionId=4&page=1&pageSize=12&sort=name
GET /mems-public/api/mems-api/api/burnaby/candidates/{id}/photo
```

Resolve actual position/candidate IDs from the API; IDs in examples are illustrative. The client uses the existing server search/filter/order/page implementation. `name` sorts ascending; `created` sorts newest first; server tie-breaking remains CandidateId. The overview only requests three candidate previews; directory requests are bounded/paged. Position and candidate details remain scoped by the URL slug. The client clears its previous state and cancels disposed-page requests; tenant ownership checks in controllers/services/EF/SQL remain authoritative. Defensive DTO checks also prevent a mismatched response from being displayed.

Every public API and media request explicitly uses browser credentials `Omit`. An Admin cookie for localhost must not change anonymous public discovery/browsing or cause another tenant's public portraits to fail. Media bytes are fetched through the same credential-free HTTP client, then displayed as `data:image/...` URLs. Ordinary image element requests would otherwise include same-origin cookies. No administrator credentials, tokens or connection strings are needed by the public app. Existing authenticated API access still enforces the actor's tenant assignment; no authentication scheme or bypass was introduced.

Media stays on existing scoped API endpoints. JPEG portraits, trusted packaged SVG avatars/logos and uploaded sanitized PNGs render through the same path. Unsupported MIME types, missing/broken responses or missing photos use a labeled fallback. Current row versions are appended to media requests so Admin image changes refresh. Upload validation and server asset storage are unchanged. No local filesystem URL is displayed.

Public errors are mapped to fixed safe messages for 400, 404 and server/unavailable failures; raw server details are not rendered. Retry reloads from the API. API responses already carry no-store policies; normal refresh retrieves current Admin changes. Unknown IDs cannot be used to mix position/candidate ownership. The client never queries SQL or reimplements database access.

## UI and accessibility

The public site uses Bootstrap with a navy/gold identity distinct from Admin, a responsive original SVG community illustration, dynamic tenant accents, election summaries, cards and profiles. All site fonts/assets are local; the application does not need an external font/CDN. Branding/biographies/images are the existing fictional assignment demonstration data, labeled accordingly.

Main navigation, tenant navigation, position/candidate links, clear labels, visible interactive focus states, a skip link, useful headings and image alternatives support keyboard browsing. Loading, empty, missing-image, validation/error and retry states are included. Dates are election information rather than claims that a past demonstration election is upcoming. Public contact links use normal HTML encoding; external websites open with `noopener noreferrer`.

## Verify

With Aspire running and the workspace browser dependencies already available:

```powershell
node scripts/verify-phase5.cjs
node scripts/verify-phase5-navigation.cjs
```

This suite browses the real WASM/API, checks images/routing/filtering/security, and uses legitimate Admin forms for disposable synchronization records. Its cleanup uses only its unique ownership prefix and preserves original data. Private ledger/error state stays under ignored `.local`; public results/screenshots are under ignored `artifacts/phase5`.

For the existing guarded record/password fingerprint check, reuse its existing fixture workflow (normal temporary Identity account, Development-only CLI, intended database-name guard):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode PreparePhase3
node scripts/verify-phase5.cjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode CompletePhase3
```

If a fixture state file already exists from an interrupted/paused verification, complete that fixture instead of overwriting it. Original account passwords are never reset. Other phase regressions keep their documented workflows. Phase 1's probe now visits the root public `/_status` page, retaining its same-origin API, health, privacy and dependency checks.

Detailed observed verification and exact file changes are in `phase5-verification.md` and `phase5-files.txt`.

## Phase 6 boundary

No ballot selection buttons/state, saved choice list, vote submission, running totals, selection storage or printable selected guide is implemented. No ballot table or API endpoint was created. Candidate search/filter state in URL query parameters is directory navigation, not ballot choices.

The rubric conflicts: page 6 mentions Local Storage, while pages 7–8 specifically require browser Session Storage, no transmission of selections to backend APIs, no SQL storage and removal on session end. Phase 6 should follow those more specific Session Storage privacy requirements unless the instructor clarifies otherwise. That work requires separate approval. No Git, Azure, COMP4977, deployment or submission ZIP actions are part of Phase 5.
