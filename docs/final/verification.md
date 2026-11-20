# Final local verification — Phase 7

Fresh verification completed November 17, 2026 (Pacific). The existing application features were preserved: all **151 files under the seven application projects** match the Phase 7 starting hashes. No new application project, entity, API, migration, role policy or credential was introduced. Historical Phase 1–6 reports remain available separately; results below identify current Phase 7 checks.

| Check | Fresh result / evidence |
| --- | --- |
| Complete restore/build | Seven .NET 10 projects; **0 warnings, 0 errors** (`artifacts/phase7/restore.txt`, `build.txt`) |
| Installed toolchain/start | SDK 10.0.402; Aspire CLI 13.6.4; actual `aspire run` startup/restart with existing certificate and retained resources |
| Package/model check | No known vulnerable packages reported for all seven projects including transitives; no pending EF model changes |
| Infrastructure regression | Five groups passed: SQL-backed readiness, real Admin interactivity, WASM/reload/gateway/config privacy and dashboard logs/traces |
| Database/Identity regression | Original five tenants, 15 positions, 30 candidates, five users, seven roles; scoped queries, Identity assignments/hashing, **20 SQL constraint rejections**, restrictive deletes, rollback reseed/lockout and idempotence passed |
| API regression | Nine groups passed for CRUD, validation, parent/tenant ownership, forged IDs, concurrency, media, OpenAPI, safe DTO/errors, cookies, CSRF/origin, logout and owned-account lockout |
| Browser API security | Four groups passed: legitimate login, own/foreign data/media, CSRF rejection/logout and HttpOnly boundaries |
| Admin regression | Nine browser groups and four accessibility/observability groups passed; all five original logins, scoped totals/CRUD/media, owned password workflow, tenant/circuit/session checks and cleanup |
| Public regression | Nine main groups and five navigation groups passed for all tenants, portraits/avatars/logos, directories/profiles, search/filter/order/paging, history/refresh/errors, anonymous media, Admin sync and delayed tenant changes |
| Personal guide/privacy | **11 groups passed**, **123 observed anonymous GET API requests**, zero browser page errors; choice changes generated zero API requests and identical catalog traffic for empty/populated guides |
| Guide storage cases | Duplicate/concurrent add/remove, tenant-only clear confirmation, same-tab refresh, independent/opener tabs, stale/foreign IDs, invalid/blocked/quota storage and safe metadata failure passed |
| Guide live data sync | One owned candidate selected, edited to a 100-character name/new position and deleted; refreshed guide reflected changes/pruned stale ID. Record cleaned; public choices never written to API |
| Printing/PDFs | Six fresh Letter/A4 outputs passed dimensions/required-field/control/page-break inspection; populated/empty documents one page, 32 long-name rows five pages on each size |
| Native print preview | Fresh workspace browser profile passed Letter and A4; cancelled without printing. An initial reused-profile automation context closed; the clean-profile retry passed without application changes |
| Real screenshot/API verification | All five categories captured; actual API statuses **200,400,404,401,403**; live OpenAPI contains **29 operations**; SQL export is clearly labelled real read-only query results, not SSMS or a mockup |
| SQL metadata | Exactly **12 original tables**, **two original migrations**, no ballot/vote/selection columns; actual column/key/FK dictionary exported without Identity row values |
| Actual restart | Same SQL container and Aspire restarted; owned candidate contents/date/row version, changed test password/hash and whole probe fingerprint survived; only owned probes removed |
| Browser/auth restart | A real open tab retained its local choice across restart; a legitimately logged-out cookie remained rejected |
| Record/password preservation | Election, user, role/membership and seed-history record fingerprint matches the original completed Phase 2 checkpoint; no active fixtures remain |
| Source/secrets | All 151 application files, original Phase 1–6 prompts and both development secret files match starting hashes; **434 public source/built assets** contain none of the known development secrets |
| Assignment 1 | All **2,732 files** match SHA-256, size and last-write-time baseline, including SQLite and Git files; no application commands or writes there |

The current retained container is `mems-assignment2-sql`, ID `e7fd02edfdaa499fb8c6ca179b91c3e680cd069787270457bdda608932ec7a26`; volume `mems-assignment2-sql-data` mounted at `/var/opt/mssql`; database `MEMSAssignment2`. No Phase 7 replacement occurred. The older Phase 3 replacement of the original Phase 2 instance is acknowledged, not claimed undone. Original row values/hashes are compared; SQL allocation counters can advance during owned/rollback tests, so byte-identical database files are not asserted.

The schema migrations remain `20261116003601_InitialSqlServerIdentityTenancy` and `20261116004505_FixHyphenatedSlugsAndAssetNames`. API initialization remains limited to the intended Development database and completed-version seeds. No schema reset, volume deletion, imports or original-password reset occurred.

Machine-readable results and debug outputs are ignored under `artifacts/phase7`; sanitized SQL/OpenAPI/API exports and curated screenshots are under `docs/final/evidence`. Source/private test baselines and fixture credentials remain ignored under `.local`. The publication audit checks the proposed source/report allowlist without initializing Git or creating an archive. The final source inventory and dry-run manifest accompany this report.

No functional application blocker was found in the exercised checks. Physical printers, Safari/Firefox, real phones, full screen-reader/WCAG audit and a clean-machine first run were not tested. Written instructor feedback, actual Copilot activity and individual contribution allocation are unavailable. GitHub repository/access, a final submission ZIP, archive extraction/upload and publishing remain unperformed pending approval. No Azure deployment is required or performed.
