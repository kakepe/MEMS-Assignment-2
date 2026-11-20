# Phase 7 read-only audit

Audit completed November 17, 2026 (Pacific) before documentation changes. Working directory and writable project: `C:\Users\Asma\Documents\MEMS-Assignment-2`. Assignment 1 is a read-only reference. Sources: the complete 16-page Assignment 2 PDF, actual source/configuration, existing Phase 1–6 reports/evidence and the user's current authorization. No application feature rewrite is planned.

## Remaining deliverables and reusable evidence

| Requirement | Existing reusable material | Phase 7 work / gap |
| --- | --- | --- |
| Seven-project .NET solution | MEMS.sln; all seven approved .NET 10 projects | Fresh restore/build; preserve every `src/` file |
| Architecture diagram (pp15–16) | AppHost source; phase reports explain the gateway, Admin/Data and Public/API paths | Create rendered architecture showing orchestration, actual data flows and Burnaby/Surrey/Richmond isolation; embed in final PDF |
| Updated ERD (p16) | `phase2-schema.md` Mermaid, SQL migrations, EF configurations; Phase 6 schema JSON | Validate actual SQL metadata, render current domain/Identity ERDs, embed and include data dictionary |
| Technical documentation | README; Phase 2 seeds/schema; Phase 4 Admin, Phase 5 Public and Phase 6 guide docs | Consolidate current run/configuration/security/privacy/backup-preservation guidance; distinguish historical phase descriptions |
| API documentation (p9) | `/api/docs`, OpenAPI, `phase3-api.md` and tested DTO/CRUD examples | Verify current paths/models/status codes against live OpenAPI; publish a current reference without old scaffold language |
| Deficiency report (p13) | User reports 97/100, three marks lost because report lacked ERD | Document only that reported issue and embed ERD. No written marking feedback found; no invented validation/security deductions |
| Required screenshots (p15) | Real Aspire/Admin/Public/print captures and API/SQL result JSON in ignored artifacts | Refresh and curate five categories, including actual API test responses and actual read-only SQL results; retain provenance |
| Privacy (pp6–8) | Session-only implementation; 11 browser groups, six PDF checks and native preview evidence | Repeat browser/privacy checks; explain p6 Local Storage vs pp7–8 Session Storage; no backend choices |
| Build/Aspire/regressions | Existing reproducible scripts and Phase 1–6 JSON | Fresh full solution build, `aspire run`, regressions, schema/persistence/credential/Assignment 1 checks |
| Copilot/AI (p14) | `.github/copilot-instructions.md`, actual phase prompt Markdown; confirmed Codex assistance | Review files; document actual Codex tasks/examples/limitations. Actual Copilot use unverified; student confirmation requested |
| PDF/Word documentation (p15) | Engineering Markdown only; no consolidated report PDF/Word | Produce reviewable report PDF, editable source and diagrams with screenshots |
| Instructor-accessible GitHub (p16) | Git executable installed, but **no Git repository exists here** | No tracked/index/untracked Git status is available; inventory workspace candidates and plan private/instructor-accessible publishing. No init/commit/push authorized |
| Safe source submission | `.gitignore` excludes runtime/build/secret outputs | Audit eligible source/curated docs; prepare hashed allowlist and exclusion plan. Do not create final ZIP |
| Team attribution | Existing inherited names are not reliable evidence of current team | Student names/contributions requested; mark pending if no response rather than infer from paths/headers |
| Azure | PDF explicitly says no Azure deployment; professor repeated this | No Azure work; document local Aspire startup |

## Initial environment findings

Installed SDK: 10.0.402; Aspire CLI: 13.6.4. Docker Engine was stopped at audit time; runtime checks have not yet run for Phase 7. Existing Phase 6 results are historical evidence, not a claimed Phase 7 pass. Start only the existing Docker Desktop/retained container; do not switch engines, update settings or recreate storage.

The current `.gitignore` excludes `.local`, `.tools`, `artifacts`, bin/obj, node_modules, development appsettings, SQLite/database files and `.env` files. Curated report evidence must live separately from raw runtime artifacts and pass secret review. Ignore rules alone would not remove already tracked secrets; here there is no repository/index to inspect. No Git initialization was performed.

## Scope and evidence boundaries

Written Assignment 1 feedback is missing. The only confirmed conversational feedback is the user's reported 97/100 and missing ERD; other code deficiencies must not be attributed to the instructor. Team attribution and actual Copilot activity remain unverified pending student response. The planned report will identify those gaps explicitly. No Azure deployment, final ZIP or GitHub action is part of this authorization.
