# Phase 1 observed verification

Verified locally on November 15, 2026. This is an engineering checkpoint, not the final assignment report.

| Check | Observed result |
| --- | --- |
| Workspace | C:/Users/Asma/Documents/MEMS-Assignment-2 |
| SDK | .NET 10.0.402; runtime 10.0.13 |
| Docker | Engine 29.9.3, Docker Desktop Linux/amd64, accessible outside the tool sandbox |
| Aspire CLI | 13.6.4, commit c89352703f78ae46de90fbf751ed8b52b77b89bf |
| Templates | Aspire.ProjectTemplates 13.6.4 installed into workspace-local custom hive; Blazor/API flags checked |
| Restore | All seven projects restored after resolving patch dependencies |
| Final Debug build | Succeeded, 0 warnings and 0 errors |
| NuGet audit | No vulnerable packages reported in all seven projects, including transitive dependencies |
| Dashboard resources | SQL container, database, Admin, API, gateway and Public all Running |
| SQL Server | MEMSAssignment2 reachable; SELECT DB_NAME() confirmed target; sys.tables showed 0 user tables |
| Storage | mems-assignment2-sql-data mounted at /var/opt/mssql; existing container/volume survived AppHost restarts |
| API | Foundation probe, /health, /alive and OpenAPI returned 200; probe reported real SQL connectivity |
| Admin | Browser rendered /admin; Interactive Server button called API through service discovery successfully |
| Public | WASM rendered/reloaded; button called API using only same-origin HTTPS gateway requests |
| Browser boundary | No SQL password, dashboard token or connection-string keys in tested configuration; no Data, EF or ServiceDefaults library in WASM assets |
| Observability | Actual structured probe messages and GET api/foundation traces visible in authenticated dashboard |
| Assignment 1 | All 2,732 files matched SHA-256, size and last-write baseline; no changes |
| Existing containers | mysqldb and mng retained their pre-existing stopped status; neither was modified or deleted |

After verification, the AppHost/application processes were stopped. The SQL container was also stopped (final state: exited); its container and `mems-assignment2-sql-data` volume were retained. Start with `scripts/start.ps1 -NoBuild` to reopen the local URLs.

Actual browser verification output: `artifacts/phase1/verification.json`. Screenshots: admin.png, public.png, dashboard.png, structuredlogs.png, traces.png. These ignored artifacts are genuine captures, not final submission evidence for later functionality.

Issues resolved: sandbox Docker/CLI/NuGet restrictions through approved execution; custom template hive needed an absolute path; initial vulnerable dependency versions patched; readiness moved from WASM asset resource to gateway; Admin prerender interaction guarded; Public base path aligned with gateway mount. Dashboard verification filters the virtualized telemetry grid instead of assuming every event is rendered at once.

No outstanding Phase 1 runtime blocker. Known limitations: gateway integration remains preview; browser tests tolerate development certificate errors in their isolated context, so they do not prove trust on another machine; authentication/tenancy and final tenant routes are not implemented. The SQL password remains local-development-only in ignored configuration. No schema migrations, seeds, production configuration, cloud deployment or final report was created.

The launch helper bypasses CLI certificate automation. The initial standard dotnet run invoked Aspire's certificate-check step; no explicit certificate-install/trust command was issued by this project. No system-wide package or template installation was requested.
