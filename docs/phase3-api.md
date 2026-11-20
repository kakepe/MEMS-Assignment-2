# Phase 3 API reference

This is the local engineering reference for the implemented REST API. It is not the final assignment report. The seven-project architecture, SQL Server schema, two existing migrations, seed batches and administrator assignments are retained from Phase 2. Finished Blazor login and CRUD screens remain Phase 4 work.

Direct API base: `https://localhost:7192`. Readable documentation: `/api/docs`. Machine-readable OpenAPI: `/openapi/v1.json`. The Public gateway forwards the API through `https://localhost:7290/mems-public/api/mems-api/api/...`; browser clients can use that origin without cross-origin requests. `/mems-public/` remains the temporary WASM hosting mount. DTO media URLs and Location headers use canonical direct-API paths such as `/api/burnaby/candidates/1/photo`. Gateway clients prefix these with `/mems-public/api/mems-api`, rather than resolving them against the gateway root. The browser verification exercises this routing; final UI clients must retain it.

## Endpoints

`{slug}` is the normalized, lowercase municipality slug. `{id}` always belongs to the resource in that route. A foreign ID under an otherwise permitted tenant returns 404. An authenticated municipality administrator requesting another tenant receives 403, including public GET routes. Unknown/invalid slugs return 404. Anonymous election reads are allowed; the public directory is deliberately a narrow discovery projection.

| Method | Route | Permission | Success |
| --- | --- | --- | --- |
| GET | `/api/municipalities` | Public directory; signed-in municipal admins see their assignment | 200 directory array |
| GET | `/api/{slug}/municipalities` | Public tenant details | 200 array containing that municipality |
| GET | `/api/{slug}/municipalities/{id}` | Public tenant details | 200 municipality + ETag |
| GET | `/api/admin/municipalities` | Super Admin | 200 municipality array |
| GET | `/api/admin/municipalities/{id}` | Super Admin | 200 municipality + ETag |
| POST | `/api/admin/municipalities` | Super Admin + CSRF | 201 municipality + Location + ETag |
| PUT | `/api/admin/municipalities/{id}` | Super Admin + CSRF + If-Match | 200 municipality + ETag |
| DELETE | `/api/admin/municipalities/{id}` | Super Admin + CSRF + If-Match | 204 |
| GET | `/api/{slug}/positions` | Public tenant election data | 200 position array |
| GET | `/api/{slug}/positions/{id}` | Public tenant election data | 200 position + ETag |
| POST | `/api/{slug}/positions` | Assigned municipal admin or Super Admin + CSRF | 201 position + Location + ETag |
| PUT | `/api/{slug}/positions/{id}` | Assigned admin or Super Admin + CSRF + If-Match | 200 position + ETag |
| DELETE | `/api/{slug}/positions/{id}` | Assigned admin or Super Admin + CSRF + If-Match | 204 |
| GET | `/api/{slug}/candidates` | Public tenant election data | 200 paged candidates |
| GET | `/api/{slug}/candidates/{id}` | Public tenant election data | 200 candidate + ETag |
| POST | `/api/{slug}/candidates` | Assigned admin or Super Admin + CSRF | 201 candidate + Location + ETag |
| PUT | `/api/{slug}/candidates/{id}` | Assigned admin or Super Admin + CSRF + If-Match | 200 candidate + ETag |
| DELETE | `/api/{slug}/candidates/{id}` | Assigned admin or Super Admin + CSRF + If-Match | 204 |
| GET | `/api/{slug}/candidates/{id}/photo` | Public tenant media | 200 image; 404 if absent |
| POST | `/api/{slug}/candidates/{id}/photo` | Assigned admin or Super Admin + CSRF + If-Match | 200 updated candidate + ETag |
| DELETE | `/api/{slug}/candidates/{id}/photo` | Assigned admin or Super Admin + CSRF + If-Match | 200 updated candidate + ETag |
| GET | `/api/{slug}/municipalities/{id}/logo` | Public tenant media | 200 image; 404 if absent |
| POST | `/api/{slug}/municipalities/{id}/logo` | Super Admin + CSRF + If-Match | 200 updated municipality + ETag |
| DELETE | `/api/{slug}/municipalities/{id}/logo` | Super Admin + CSRF + If-Match | 200 updated municipality + ETag |
| GET | `/api/auth/csrf` | Public; HTTPS required | 200 token + matching secure cookie |
| POST | `/api/auth/login` | Valid credentials + CSRF; HTTPS required | 200 safe session + Identity cookie |
| GET | `/api/auth/me` | Authenticated administrator; HTTPS required | 200 safe session |
| POST | `/api/auth/logout` | Authenticated administrator + CSRF; HTTPS required | 204 |
| GET | `/api/foundation` | Public temporary connectivity probe | 200 SQL connectivity summary |
| GET | `/health`, `/alive` | Development readiness/liveness | 200 when healthy |
| GET | `/api/docs`, `/openapi/v1.json` | API documentation | 200 |

Creating a municipality also creates its tenant-linked named Admin role in the same transaction. Role names are display/group labels; authorization uses the user's current stored scope, municipality assignment and base role. Renaming a municipality retains the original role label and its tenant ID. Municipality deletion is rejected while election records or assigned administrators/memberships remain. Position deletion is rejected while candidates reference it. No account-management, saved-vote or ballot endpoint is exposed.

## Authentication and browser use

1. GET `/api/auth/csrf`, retaining its cookie and `requestToken`.
2. POST `/api/auth/login` with JSON `{ "userName": "Burnabyadmin", "password": "<local development credential>" }` and header `X-CSRF-TOKEN: <requestToken>`. Obtain the actual credential locally, never from this document or browser configuration.
3. Retain the issued `.MEMS.Assignment2.Identity` cookie. GET `/api/auth/csrf` again: antiforgery request tokens are bound to the principal and must be refreshed after login/logout.
4. Send that new token in `X-CSRF-TOKEN` with every POST, PUT or DELETE, including logout and image uploads. Also send current `If-Match` for updates/deletes/media changes.

Identity uses the existing SQL store, password hashes, lockout (five failures, fifteen minutes), DPAPI-protected local data-protection keys and cookie name. The session cookie is Secure, HttpOnly, SameSite=Lax; antiforgery's secure HttpOnly cookie uses SameSite=Strict. Modifications and authentication require HTTPS. The API returns 401/403 JSON instead of HTML login redirects. Sign-in is limited to 30 attempts per minute per remote IP in addition to account lockout. Every request rechecks the security stamp; privileged service calls re-read the stored scope, assignment and roles. No client header/DTO can grant a role or select an administrative tenant.

No cross-origin CORS allowance is enabled. Same-origin WASM gateway requests carry cookies with the normal same-origin browser policy. The CSRF origin check additionally accepts the three existing HTTPS localhost development origins; it does not grant CORS permission. Outside Development, only the request's own origin is accepted. Phase 4 must integrate the Admin browser login flow with this cookie/CSRF design and must not copy credentials into Blazor client configuration. No JWT or iOS deployment assumption was introduced.

## JSON contracts and examples

Only `MEMS.Shared` DTOs cross the API boundary. DTOs contain public election contact details, IDs and opaque concurrency versions, but no EF navigations, Identity rows, password hashes, stamps, internal asset names, SQL details or filesystem paths. Unknown JSON properties are rejected, including attempted `municipalityId`, `dateCreated` or `photoFileName` overposting on candidate writes. A query-string tenant ID is never used for scoping.

Municipality POST/PUT request:

```json
{
  "slug": "example-town",
  "name": "Example Town",
  "province": "British Columbia",
  "electionDate": "2026-10-17",
  "description": "Fictional election demonstration.",
  "brandColor": "#245678",
  "publicTitle": "Example Town election guide"
}
```

Municipality response (illustrative IDs/versions; use the actual response):

```json
{
  "municipalityId": 42,
  "slug": "example-town",
  "name": "Example Town",
  "province": "British Columbia",
  "electionDate": "2026-10-17",
  "description": "Fictional election demonstration.",
  "brandColor": "#245678",
  "publicTitle": "Example Town election guide",
  "logoUrl": null,
  "version": "AAAAAAAAAAE="
}
```

The directory returns only `municipalityId`, `slug`, `name`, `brandColor`, `logoUrl`. Position POST/PUT request:

```json
{ "name": "Mayor", "seatsAvailable": 1 }
```

Position response:

```json
{ "positionId": 73, "municipalityId": 42, "name": "Mayor", "seatsAvailable": 1, "version": "AAAAAAAAAAI=" }
```

Candidate POST/PUT request (the position must belong to the URL municipality):

```json
{
  "positionId": 73,
  "name": "Fictional Candidate",
  "profile": "A fictional biography for local demonstration.",
  "email": "candidate@example.org",
  "website": "https://example.org"
}
```

Candidate response:

```json
{
  "candidateId": 108,
  "municipalityId": 42,
  "positionId": 73,
  "name": "Fictional Candidate",
  "profile": "A fictional biography for local demonstration.",
  "email": "candidate@example.org",
  "website": "https://example.org",
  "dateCreated": "2026-11-16T04:00:00Z",
  "photoUrl": null,
  "isFictionalSample": false,
  "version": "AAAAAAAAAAM="
}
```

GET `/api/burnaby/candidates?search=Vivian&positionId=1&page=1&pageSize=12&sort=name` returns `{ "items": [ ... ], "page": 1, "pageSize": 12, "totalCount": 1, "totalPages": 1 }`. Resolve the tenant's real position IDs first. Search is a parameterized name substring query under SQL Server's configured collation. Sort `name` is ascending; `created` is newest first; both break ties using CandidateId. Page size is 1..100; page is 1..1,000,000. The empty list has `totalPages: 0`. Invalid/foreign position filters return 400. Pagination has deterministic ordering; concurrent edits can still change the contents between requests.

Single-resource reads/create/update return `ETag: "<version>"`. Supply the entire quoted value as `If-Match`. Missing or malformed versions return 400; outdated versions/races return 409. New requests derive ownership and creation date on the server. Candidate names need not be unique (different people may share a name); municipality names/slugs and position names within a tenant are constrained unique.

Validation runs through API model binding/DataAnnotations, service-level contract checks and the existing SQL constraints. Name limits are 100; biography 4,000; municipality description 2,000; public title 120; province 50; slug 80, normalized ASCII lowercase with single hyphen separators and reserved routes rejected. Election years must be 2000..2199. Seat counts are 1..100. Optional emails are limited to 254 and validated; websites to 300 and absolute HTTP/HTTPS URLs. Blank optional contacts become null. JSON text is data; later UI rendering must retain normal HTML encoding.

## Media

POST the multipart field `file` to the photo/logo route, with CSRF and If-Match headers. A valid JPEG or PNG up to 2 MiB (2,097,152 bytes) is required. Filename paths, unsupported extensions, empty/corrupt content, mismatched decoded formats, animation, dimensions above 6,000 on either axis or over 16 megapixels are rejected. MIME labels are not trusted. Decoded images are resized to at most 1,200 on their longest side and re-encoded as PNG, stripping metadata/trailing content. Each file gets a generated GUID name in `.local/media/tenants/{municipalityId}`; only the authorized row's media endpoint is public. Sensitive local configuration and keys are outside that media directory. Total multipart request size is bounded at 3 MiB; temporary buffering stays in ignored `.local/http-temp`.

Replacement validates ownership/current version before writing. A failed database save cleans its newly created file. Successful replacement/delete only removes application-generated tenant files; bundled seed portraits/logos are never deleted. Existing seeded SVG avatars/logos are allowlisted and served as images with CSP sandbox and nosniff; new SVG uploads are rejected. Global `/images/...` static-file serving is disabled. Responses expose only the scoped media URL.

## Errors and observability

Failures return JSON Problem Details with `status`, `title`, safe `detail` and `traceId`; field validation additionally includes `errors`. Framework model-binding/CSRF responses use the same Problem Details structure, with trace metadata where provided by MVC. Typical errors: 400 invalid model/parent/CSRF/version, 401 missing/invalid login, 403 denied role/tenant/origin, 404 missing scoped resource, 409 duplicate/concurrency/dependent data, 413 upload too large, 429 sign-in rate limit, 500 generic server error. Raw exceptions, SQL messages, paths, tokens and credentials are not returned. The global handler logs status, error type and trace identifier; sensitive EF command/update logging is suppressed. Existing Aspire logs, traces and readiness remain available.

Example error:

```json
{ "status": 403, "title": "Forbidden", "detail": "This municipality is outside your assignment.", "traceId": "<request trace>" }
```

## Repeat verification

Start the existing stack, then run from the workspace:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode PreparePhase3
node scripts/verify-phase3.cjs
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-phase2.ps1 -Mode CompletePhase3
node scripts/verify-phase3-browser.cjs
```

The HTTP suite uses real Identity passwords/cookies and CSRF tokens. CRUD runs only on owned records, cleaned in `finally`. Its ignored ledger records their IDs for recovery; never remove unrelated rows to resolve failures. The CLI creates/removes a normal owned lockout-test user, checks the entire existing record/password fingerprint, and verifies generic 500 handling in-process. These are Development-only commands in the existing API executable, guarded to `MEMSAssignment2`, not HTTP endpoints or authentication bypasses. Private fixture credentials remain in ignored `.local`; retain unfinished fixture state until cleanup succeeds. Do not run against another environment. Phase 2's verification script remains available separately; its seed assertions target this unchanged demo baseline.
