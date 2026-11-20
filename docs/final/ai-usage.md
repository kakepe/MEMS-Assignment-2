# AI assistance and prompt summary

Team: **Abdullah Rehan and Alex Fisher**, confirmed by the user. The session history and checked project evidence establish **OpenAI Codex** assistance. Actual GitHub Copilot use has not been confirmed; the presence of `.github/copilot-instructions.md` and prompt files does not prove Copilot activity. Individual student contribution allocation was not supplied and is not inferred.

The following concise summaries use the actual Phase 1–6 requests and preserved prompt files. Each covers the request, technologies and observed output without reproducing the lengthy original prompts. Original prompt files are unchanged.

## Phase 1 — Foundation

We asked Codex to create a compiling, locally runnable foundation with exactly seven .NET 10 projects, Blazor Server Admin, standalone WASM Public, Web API, SQL Server and Aspire. Codex used the existing SDK/Aspire tooling, PowerShell, NuGet restore/build, Docker and browser checks to scaffold references, browser-safe gateway routing, health checks, logging and tracing. It helped verify Admin/Public/API connectivity and persistent Assignment 2 SQL storage while keeping Assignment 1 read-only (`docs/phase1-verification.md`).

## Phase 2 — Data, Identity and seeding

We asked Codex to adapt the original election entities and fictional sample content into SQL Server/EF Core, add multi-tenant relationships and Identity accounts, and preserve data across repeat seeding. EF Core CLI, SQL Server, Identity, Code First migrations, PowerShell and focused database checks produced two reviewed migrations, constrained tenant relationships and idempotent seed batches for five municipalities and 30 candidates. Codex helped verify hashing/roles, negative SQL constraints, scoped queries and actual restart persistence (`docs/phase2-verification.md`).

## Phase 3 — API security and isolation

We asked Codex for tenant-aware REST CRUD, shared DTO validation, legitimate Identity authentication/authorization, secure media handling and OpenAPI. ASP.NET WebAPI, EF/Identity services, cookie/CSRF policies, SkiaSharp image validation and Playwright-driven HTTP/browser tests produced the documented endpoints and security checks. Codex helped verify foreign-ID rejection, actor scope, validation, safe errors, image rules and preserved records/passwords; no hypothetical iOS JWT or deployment requirement was assumed (`docs/phase3-verification.md`).

## Phase 4 — Blazor administration

We asked Codex to convert the existing administration functionality into responsive Blazor Server components at `/admin`, reusing the shared security and data services. Blazor forms, Bootstrap, Identity, short-lived EF scopes and real browser tests produced login/logout/password workflows, scoped dashboard totals and municipality/position/candidate CRUD with media. Verification covered municipal permissions, interactive-circuit/session protection, concurrency, validation, mobile layouts and cleanup of owned test records (`docs/phase4-verification.md`).

## Phase 5 — Public website

We asked Codex to build reusable tenant public experiences at root slug routes, with dynamic discovery, positions, profiles, images, API search/filtering and pagination. Blazor WASM, Shared DTOs, the existing Aspire gateway and anonymous HTTP clients produced the public pages without server/database dependencies or embedded secrets. Playwright and database-preservation checks verified all five tenants, routing/history, portraits, tenant isolation, accessibility controls and real Admin-to-Public synchronization (`docs/phase5-verification.md`).

## Phase 6 — Private voting guide and print

We asked Codex to add anonymous per-municipality guides using native browser `sessionStorage`, never sending choices to APIs, URLs, logs or SQL. WASM JS interop, Bootstrap, print CSS, native Chrome preview, Playwright and pypdf verification produced add/remove/clear behavior, stale-record handling and Letter/A4 guides containing all five required fields. Codex helped verify zero-request choice changes, selection-independent catalog loading, browser lifecycle, all prior regressions, preserved data and actual `aspire run` startup (`docs/phase6-verification.md`).

## Phase 7 — Documentation and verification

The current request asks Codex to audit real rubric deliverables, consolidate diagrams/report/API and deficiency documentation, curate real evidence, rerun verification and prepare safe publishing/submission plans. Tools used are workspace reads/edits, PowerShell, .NET/Aspire/Docker, existing browser/PDF tools and read-only Git inspection; no Git initialization, commit, push, Azure deployment or submission ZIP is authorized. The final verification report distinguishes current test outcomes from historical evidence and lists unresolved attribution/feedback gaps.

## Benefits, problems and review responsibility

Codex helped turn the staged requests into concrete source changes, reproducible tests and documentation, and exposed errors through actual builds/browser/database checks rather than claiming unperformed success. Documented problems include package compatibility/security patch selection, the historical Phase 3 container-instance replacement while retaining its volume, root gateway routing, Blazor binding/focus behavior and test-harness/native-preview adaptation. No productivity percentages or time savings were measured. All generated outputs require student review; these records establish Codex assistance, not instructor approval or a Copilot usage claim.
