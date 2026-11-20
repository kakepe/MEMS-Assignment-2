# Rubric coverage and submission-readiness checklist

The marking guide on page 16 totals **100 marks**. This matrix maps requirements to implementation/evidence; it does not predict a grade or award marks.

| Category | Marks | Implementation / evidence | Review status |
| --- | --- | --- | --- |
| Multi-tenant architecture and isolation | 10 | Root tenant routes; stored actor assignments; scoped services/EF; composite SQL ownership; architecture and ERDs; foreign-ID tests | Implemented and verified locally |
| Blazor Admin application | 15 | Shared /admin, real Identity workflows, dynamic dashboards, CRUD/media/search/paging, safe circuit lifetimes | Existing feature set preserved; fresh regression |
| Election privacy / Session Storage | 5 | Native browser sessionStorage, local-only choices, neutral catalog queries, no SQL/API storage, lifecycle/error/print tests | Explicit pp7–8 privacy requirement followed; p6 wording conflict documented |
| Blazor WASM Public application | 15 | Reusable dynamic tenants, branding, directories/profiles/media, search/filter/order/pagination, guides and printing | Existing source preserved; fresh browser evidence |
| ASP.NET WebAPI | 10 | Complete domain CRUD, safe Shared DTOs, cookie/CSRF/scope, media, current OpenAPI/reference and actual statuses | Fresh API/docs verification |
| SQL Server and EF Core | 10 | Docker volume, Code First migrations, one-time seeds, Identity, constraints and services; actual metadata/data dictionary | Source unchanged; schema/data/persistence checks |
| Authentication and authorization | 10 | Super/municipal roles, canonical assignment, cookie/stamp/logout/lockout and backend tenant checks | Legitimate authentication tests; no bypass |
| .NET Aspire integration | 10 | Actual aspire run; references/readiness/health, dashboard/logs/traces, SQL orchestration | Local CLI startup verified; no Azure required |
| Assignment 1 deficiencies and best practices | 10 | Reported missing ERD corrected by embedded current ERDs; validation/security/data-quality tests documented | Written grading feedback unavailable; only user-reported deduction attributed |
| Documentation and Copilot usage | 5 | Consolidated PDF, diagrams, technical/API/schema references, screenshots, actual prompt files and concise AI summaries | Documents prepared; actual Copilot use unverified (Codex confirmed) |
| Total | 100 | No self-assigned score | Instructor assessment remains independent |

## Ready for review

- Seven-project solution, migrations, seed source and safe public assets preserved; no application feature rewrite.
- Report PDF and editable offline sources include Aspire architecture, all-table SQL ERDs, API examples/statuses, real screenshots and honest deficiency/AI records.
- Team names confirmed: Abdullah Rehan and Alex Fisher. Individual contribution allocation was not supplied or inferred.
- Required screenshot categories: Aspire, Admin, Public, API testing and SQL Server; provenance distinguishes actual app captures from actual request/query exports.
- Fresh build/startup, regressions, privacy and integrity outcomes are recorded in the verification section and machine-readable evidence.
- `.gitignore` strengthened; source/curated-document allowlist and known-secret scan prepared without staging or packaging.

## Pending explicit approval or external evidence

- Review the generated PDF, image legibility, schema accuracy, wording and all team/AI attributions.
- Written instructor feedback is unavailable; no other grading deductions or corrections may be fabricated. Reconcile actual written comments later if supplied.
- Actual GitHub Copilot activity remains unverified. The rubric requires it; the report accurately documents Codex and does not imply that instructions/prompts establish Copilot use. Student/instructor review must resolve this compliance gap.
- Instructor-accessible GitHub repository has not been initialized or created. Owner/name/access, commit and publishing must be approved before action.
- Final submission ZIP, archive inspection/extraction and upload have not occurred and require approval. No Azure deployment is required.
- Clean-machine first-run, real printer output, Safari/Firefox, real mobile devices and full screen-reader/WCAG audit were not performed; local Chrome/PDF/browser tests do not claim those results.

Stop for review here. The publishing/package plan describes later authorized steps; it is not permission to execute them.
