# Current API reference and verification

Verified against live OpenAPI and actual HTTPS request results in `evidence/openapi.json` and `evidence/api-results.json`. This current reference supersedes the historical Phase 3 scaffold/mount descriptions.

Direct base: https://localhost:7192. Public transport: https://localhost:7290/mems-public/api/mems-api followed by the canonical /api/... path. Root tenant UI lives at https://localhost:7290/{slug}; the legacy /mems-public/ entry redirects to the root.

## Complete live OpenAPI operations

| Method | Route | Documented statuses |
| --- | --- | --- |
| GET | `/api/auth/csrf` | 200 |
| POST | `/api/auth/login` | 200, 400, 401, 403, 429 |
| GET | `/api/auth/me` | 200, 401, 403 |
| POST | `/api/auth/logout` | 204, 400, 401 |
| GET | `/api/{slug}/candidates` | 200, 400, 401, 403, 404, 409, 500 |
| POST | `/api/{slug}/candidates` | 201, 400, 401, 403, 404, 409, 500 |
| GET | `/api/{slug}/candidates/{id}` | 200, 400, 401, 403, 404, 409, 500 |
| PUT | `/api/{slug}/candidates/{id}` | 200, 400, 401, 403, 404, 409, 500 |
| DELETE | `/api/{slug}/candidates/{id}` | 204, 400, 401, 403, 404, 409, 500 |
| GET | `/api/foundation` | 200 |
| GET | `/api/{slug}/candidates/{id}/photo` | 200, 400, 401, 403, 404, 409, 413, 500 |
| POST | `/api/{slug}/candidates/{id}/photo` | 200, 400, 401, 403, 404, 409, 413, 500 |
| DELETE | `/api/{slug}/candidates/{id}/photo` | 200, 400, 401, 403, 404, 409, 413, 500 |
| GET | `/api/{slug}/municipalities/{id}/logo` | 200, 400, 401, 403, 404, 409, 413, 500 |
| POST | `/api/{slug}/municipalities/{id}/logo` | 200, 400, 401, 403, 404, 409, 413, 500 |
| DELETE | `/api/{slug}/municipalities/{id}/logo` | 200, 400, 401, 403, 404, 409, 413, 500 |
| GET | `/api/municipalities` | 200, 400, 401, 403, 404, 409, 500 |
| GET | `/api/{slug}/municipalities` | 200, 400, 401, 403, 404, 409, 500 |
| GET | `/api/{slug}/municipalities/{id}` | 200, 400, 401, 403, 404, 409, 500 |
| GET | `/api/admin/municipalities` | 200, 400, 401, 403, 404, 409, 500 |
| POST | `/api/admin/municipalities` | 201, 400, 401, 403, 404, 409, 500 |
| GET | `/api/admin/municipalities/{id}` | 200, 400, 401, 403, 404, 409, 500 |
| PUT | `/api/admin/municipalities/{id}` | 200, 400, 401, 403, 404, 409, 500 |
| DELETE | `/api/admin/municipalities/{id}` | 204, 400, 401, 403, 404, 409, 500 |
| GET | `/api/{slug}/positions` | 200, 400, 401, 403, 404, 409, 500 |
| POST | `/api/{slug}/positions` | 201, 400, 401, 403, 404, 409, 500 |
| GET | `/api/{slug}/positions/{id}` | 200, 400, 401, 403, 404, 409, 500 |
| PUT | `/api/{slug}/positions/{id}` | 200, 400, 401, 403, 404, 409, 500 |
| DELETE | `/api/{slug}/positions/{id}` | 204, 400, 401, 403, 404, 409, 500 |

Health/liveness and readable docs may be excluded from the generated operation list: `/health`, `/alive`, `/api/docs`; machine-readable spec is `/openapi/v1.json`. These are operational endpoints, not missing domain CRUD.

## Authentication and tenancy

Anonymous users can read public discovery/election/media DTOs. Authenticated municipal API requests remain restricted to the stored assignment even on GET; the Public app deliberately omits credentials for anonymous information browsing. Super Admin alone manages municipalities; positions/candidates require the assigned municipal actor or Super Admin. Unknown slugs and foreign IDs fail safely. Client-supplied IDs or role headers cannot grant scope.

Legitimate cookie flow: GET `/api/auth/csrf`; POST `/api/auth/login` with local credentials and `X-CSRF-TOKEN`; obtain a fresh principal-bound CSRF token; retain the Secure/HttpOnly Identity cookie. Modifying cookie requests require the token; PUT/DELETE/media changes also need the current quoted `If-Match` version. Do not put real credentials, cookie values or tokens in shared examples or a client asset. No JWT or new authentication mechanism was introduced.

## JSON examples

Candidate POST/PUT request (illustrative position ID; resolve an authorized actual parent first):

```json
{
  "positionId": 1,
  "name": "Fictional Example Candidate",
  "profile": "Public election biography.",
  "email": "candidate@example.test",
  "website": "https://example.test"
}
```

Position request:

```json
{ "name": "Councillor", "seatsAvailable": 6 }
```

Municipality request (Super Admin):

```json
{
  "slug": "example-town",
  "name": "Example Town",
  "province": "British Columbia",
  "electionDate": "2026-10-17",
  "description": "Fictional election information.",
  "brandColor": "#245678",
  "publicTitle": "Example Town election guide"
}
```

The actual public response and validation/error examples below are exported from live requests; concurrency versions are public opaque DTO fields, not credentials.

### GET https://localhost:7192/api/burnaby/candidates?page=1&pageSize=3&sort=name → 200

```json
{
  "items": [
    {
      "candidateId": 14,
      "municipalityId": 2,
      "positionId": 1,
      "name": "Adrian Solano",
      "profile": "Adrian Solano is an invented community enterprise manager seeking the mayor's office in this fictional Burnaby election. His sample background combines organizational budgeting, staff supervision, and partnerships between local services. He proposes a practical review of municipal service delivery, with plain-language information about costs, waiting times, and planned improvements. Adrian also emphasizes balancing new development with reliable infrastructure and accessible recreation spaces. He would encourage public meetings that welcome different perspectives and explain which issues require cooperation with other levels of government. His fictional interests include supporting neighbourhood business areas and making community facilities easier to navigate. Away from work, he enjoys recreational cycling and volunteering at events that bring residents together across generations.",
      "email": "candidate@example.org",
      "website": "https://example.org",
      "dateCreated": "2026-11-16T00:46:07.7416019",
      "photoUrl": "/api/burnaby/candidates/14/photo",
      "isFictionalSample": true,
      "version": "AAAAAAAANtQ="
    },
    {
      "candidateId": 15,
      "municipalityId": 2,
      "positionId": 2,
      "name": "Mei Tanaka",
      "profile": "Mei Tanaka is a fictional transportation planner and community arts volunteer running for a council position in the Burnaby sample election. Her imagined work focuses on how people reach everyday destinations, including those who walk, use mobility aids, cycle, or rely on transit. She proposes reviewing municipal street crossings, connections to public facilities, and the clarity of information about neighbourhood construction. Mei also supports accessible arts and recreation opportunities as part of healthy community life. She would ask council reports to explain how proposed projects affect different users and how their maintenance will be funded. Outside this fictional campaign, she enjoys drawing, visiting local parks, and helping volunteers organize small, inclusive neighbourhood exhibitions.",
      "email": "candidate@example.org",
      "website": "https://example.org",
      "dateCreated": "2026-11-16T00:46:07.74262",
      "photoUrl": "/api/burnaby/candidates/15/photo",
      "isFictionalSample": true,
      "version": "AAAAAAAANtU="
    },
    {
      "candidateId": 18,
      "municipalityId": 2,
      "positionId": 3,
      "name": "Patrick Rowden",
      "profile": "Patrick Rowden is an invented retired technical college administrator and literacy volunteer seeking a school trustee position in this fictional Burnaby election. His sample background includes facilities planning, program evaluation, and explaining institutional budgets. He proposes school board discussions that connect long-term spending decisions with student learning needs and equitable access to resources. Patrick supports listening to students, educators, families, and support staff before deciding which improvements should take priority. He distinguishes board oversight from the work of principals and teachers and values clear public reporting on district goals. His fictional interests include practical learning opportunities, accessible libraries, and intergenerational volunteering. Outside this demonstration campaign, he enjoys restoring furniture and participating in neighbourhood reading circles.",
      "email": "candidate@example.org",
      "website": "https://example.org",
      "dateCreated": "2026-11-16T00:46:07.7453411",
      "photoUrl": "/api/burnaby/candidates/18/photo",
      "isFictionalSample": true,
      "version": "AAAAAAAANtg="
    }
  ],
  "page": 1,
  "pageSize": 3,
  "totalCount": 6,
  "totalPages": 2
}
```

### GET https://localhost:7192/api/burnaby/candidates?positionId=5 → 400

```json
{
  "title": "Bad Request",
  "status": 400,
  "detail": "Choose a position in this municipality.",
  "traceId": "00-7f84e5560ab69ae56879ad7cd311a633-1107be0cfd629b6c-01"
}
```

### GET https://localhost:7192/api/unknown-tenant/candidates → 404

```json
{
  "title": "Not Found",
  "status": 404,
  "detail": "Municipality not found.",
  "traceId": "00-b5370cb1bc7c2f827bfc8e5a9fff27fa-bd7d202f5ef27106-01"
}
```

### POST https://localhost:7192/api/burnaby/candidates → 401

```json
{
  "title": "Unauthorized",
  "status": 401,
  "detail": "Unauthorized",
  "traceId": "00-dd287a27a508e55f7964ca70ee15eb3d-8c3b9d2a3efc9b07-01"
}
```

### GET https://localhost:7192/api/surrey/candidates → 403

```json
{
  "title": "Forbidden",
  "status": 403,
  "detail": "This municipality is outside your assignment.",
  "traceId": "00-219cc20f5efb62c8bcc593180cb369fa-4d932acaedc5be9b-01"
}
```

## Validation, concurrency and errors

Candidate queries support `search`, `positionId`, `page`, `pageSize` (1–100) and `sort=name|created`, with stable ID tie-breaking. Slugs are normalized and unique; names/biographies have bounded lengths; contact URLs/email, seats and parent ownership are validated in UI/services/API and appropriate SQL constraints. Unknown JSON properties are rejected. DTOs exclude EF navigations, Identity rows, password hashes, connection strings and filesystem names.

Expected responses include 200/201/204 success; 400 safe validation/CSRF errors; 401 unauthenticated; 403 forbidden; 404 unknown/foreign record; 409 duplicate/dependency/concurrency conflict; generic 500 without internal details. Oversized/unsupported/spoofed media is rejected and cookie modifications require HTTPS. POST/PUT/DELETE and media scenarios are exercised by the fresh regression suite, not the screenshots alone.

Full safe model definitions are included in the OpenAPI snapshot. The endpoint tests cover all domain CRUD, uploads, scope, error and authentication requirements without exposing a voter-selection endpoint.
