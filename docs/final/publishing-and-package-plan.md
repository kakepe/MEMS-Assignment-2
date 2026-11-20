# Safe GitHub publishing and submission package plan

This is a review-only plan. No Git initialization, staging, commit, push, repository creation, publishing, deployment or final ZIP has occurred. The workspace is not currently a Git repository, so there are no tracked/indexed files, remote configuration or commit history to review here. Assignment 1's Git repository must not be used or changed.

## Proposed contents

The dry-run manifest lists paths, sizes and SHA-256 hashes for reviewable source/documentation candidates. Include the seven-project solution, project/source/static assets, reviewed migrations and fictional seed data, `tests`, verification/start scripts, global SDK/NuGet configuration, README, AGENTS, `.gitignore`, original `.github` instructions/prompts and final documentation/diagrams/curated evidence. Keep historical phase documents where the final report references them. No eighth project or copied application database is needed.

Exclude `.local`, `.tools`, raw `artifacts`, all bin/obj and node_modules, local IDE settings, appsettings Development/Local/Production overrides, `.env`, secrets, private keys/certificates, SQL/SQLite files or backups, runtime logs/dumps, browser profiles, authentication/session state and generated archives. Do not copy Docker volumes, the Identity key ring or uploads. The root assignment handout and long starter prompt remain in the workspace but are excluded from the proposed allowlist; the concise actual phase prompt files remain included and unchanged. Public fictional seed images are source assets and must stay included.

`.gitignore` now additionally excludes common database exports/backups, logs/dumps, private-key/certificate formats and ZIPs. Ignore rules do not remove already tracked files; after future approved initialization, recheck the index and any staged diff. `publication-manifest.json` and `publication-audit.json` are review-only inventories excluded from their own hashes to avoid self-reference. They are not an archive or proof of published repository status.

## Approval gates and exact later sequence

1. Review the report PDF, diagrams, evidence, team names and AI/feedback gaps. Confirm course policy, repository owner/name, instructor access and whether a private repository is appropriate. No destination has been selected or created.
2. Only after explicit approval, initialize Git **inside Assignment 2**, stage paths from the reviewed allowlist rather than a blind whole-tree add, inspect staged filenames/content and repeat secret review. Do not import Assignment 1 history. A commit must receive the user's authorization.
3. Only after explicit publishing approval, create/configure the chosen instructor-accessible repository and push the reviewed commit. Verify access using the actual resulting URL; do not claim repository delivery before it exists. Keep credentials in the approved credential manager, never in a remote URL or source.
4. Only after explicit submission-package approval, regenerate the manifest, revalidate linked evidence/report/source, and create a source ZIP from the allowlist. Inspect its file list and scan the resulting archive before delivery. Do not include `.local` just to make startup convenient; provide the documented secure local configuration steps instead.
5. Validate extraction/build on a separate clean environment without reusing this machine's fixed SQL container or secrets. That clean-machine validation and ZIP inspection have not yet occurred. Upload/submission is a separate final action, not authorized here.

The technical guide documents first-run credential prompting and migration/seed initialization on a separate clean environment. Existing local credentials remain paired with the current SQL volume. Do not test a fresh copy by recreating this user's database, changing its password or clearing seed history. No Azure deployment is required or planned.

## Outstanding review items

Written instructor feedback is unavailable beyond the student's missing-ERD report. Actual Copilot use is unverified; Codex is documented accurately. Individual team contribution allocation was not provided. Repository access, final ZIP contents/extraction, clean-machine run and actual submission remain pending explicit approval and execution. Local Chrome/browser/PDF checks do not substitute for physical-printer, cross-browser or full accessibility audits.
