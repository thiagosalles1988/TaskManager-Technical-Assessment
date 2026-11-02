# Final stabilization record

This report records stabilization **before publication**. The developer subsequently authorized committing, pushing and making the repository public; see [publication authorization](evidence/DEVELOPMENT.md#publication-authorization). Historical statements below about unpublished or unstaged work describe the end of stabilization.

Executed by OpenAI Codex on 2026-11-02 (America/Sao_Paulo), against the uncommitted working tree based on `2e7a43e`. The developer confirmed Codex was used inside Visual Studio Code. This is execution evidence and AI review, not human approval or a production deployment.

## Corrections and regression evidence

| Earlier finding                                        | Correction                                                                                                                                                              | Evidence                                                                                                                                                                                                                                                     |
| ------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| M1: incompatible Accept caused error-writing failures  | `TryWriteAsync` followed by direct `application/problem+json` serialization when negotiation has no writer; original status, validation errors and safe details survive | [ProblemResponseTests](../tests/TaskManager.IntegrationTests/ProblemResponseTests.cs): Development/Production, JSON/problem/wildcard/plain/HTML/XML, validation, wrong credentials, conflict, missing resource/JWT, malformed JSON and unexpected exceptions |
| M2: offsetless timestamps depended on host timezone    | Explicit ISO seconds plus Z or numeric offset; maximum seven fractional digits; invalid/offsetless/over-precision input is 400; UTC Z output                            | [JsonContractTests](../tests/TaskManager.IntegrationTests/JsonContractTests.cs) and [ApiTests](../tests/TaskManager.IntegrationTests/ApiTests.cs): positive/negative offsets, nullable date, exact ticks through SQL/create/get/PUT/get                      |
| M3: development dependencies had advisories            | Official Angular update from 21 to core/compiler 22.2.4, CLI/build 22.2.5, TypeScript 6.0.3                                                                             | Clean npm ci, unit/build/browser checks and full/production audits; no forced peers, transitive overrides or warning suppression                                                                                                                             |
| L1: SQL assertions did not isolate individual defenses | Separate title/status inserts assert SQL error 547 and exact constraint; FK is asserted separately; forged entity passes the owner guard and reaches UPDATE SQL         | [RepositoryTests](../tests/TaskManager.IntegrationTests/RepositoryTests.cs), including unchanged victim data and repeatable schema-version count                                                                                                             |
| L2: HTTP security tests were SQL-gated                 | Additional persistence-substituted fixtures run real MVC/JWT/error pipeline without SQL; required verification can reject missing SQL configuration                     | [AuthenticationPipelineTests](../tests/TaskManager.IntegrationTests/AuthenticationPipelineTests.cs), JSON/problem fixtures and `Verify.ps1 -RequireSql`                                                                                                      |
| L3: unrelated edits lost due seconds/ticks             | Editor reuses the original server timestamp when its local due field is unchanged                                                                                       | [Editor regressions](../taskmanager-web/src/app/tasks/task-editor.component.spec.ts) and [browser cases](../taskmanager-web/e2e/taskmanager.spec.ts) assert exact seven-digit preservation, changed local time and optional clearing                         |
| L4: combined non-flags enum names were accepted        | Dedicated converter allows exactly Todo/InProgress/Completed, case-sensitive                                                                                            | JSON tests reject combined, numeric, numeric-string, null, lowercase and unsupported status values                                                                                                                                                           |

Actual test-first runs:

- Error negotiation: **16 failed /10 passed /0 skipped**, then **26 passed /0 failed /0 skipped** after correction.
- JSON/date/SQL batch: **13 failed /15 passed /0 skipped** before the converters. The subsequent full suite passed, including all 28 selected cases.
- Frontend editing: **1 failed /16 passed** before preservation; it expected `2026-11-04T15:00:45.1234567Z` but received `2026-11-04T15:00:00.000Z`. After correction, **17 passed /0 failed**.
- SQL coverage strengthening, the SQL-independent authentication fixture and new browser cases were added as verification; no separate failing-first claim is made for them.

The existing `datetime2(7)` schema and UTC parameter/reader mapping already preserve 100-nanosecond ticks. No schema/data rewrite was needed. A changed browser due field intentionally selects minute precision; unchanged fields preserve exact existing data. DST-ambiguous local wall times still follow JavaScript Date.

## Dependency decision

Before the update, `glob` **13.0.6** and `minimatch` **10.2.6** contributed to **16 moderately affected development packages** (not 16 demonstrated application exploits). Production-only npm audit already reported zero. The earlier review links the [glob advisory](https://github.com/advisories/GHSA-3g3m-2fjr-6pw4) and [minimatch advisory](https://github.com/advisories/GHSA-g3hv-75wg-m249).

The latest queried Angular 21 CLI patch still retained that dependency tree. The update used `npx ng update @angular/core@22.2.4 @angular/cli@22.2.5 --allow-dirty`, with no `--force` or automatic commits. Installed peer requirements accept Node 24.18.0, TypeScript 6.0.3 and RxJS 7.8.x. See the official [compatibility matrix](https://angular.dev/reference/versions) and [update command](https://angular.dev/cli/update).

Upstream migrations explicitly retain eager change detection and the XHR backend. Migration-added suppression of two template diagnostics was removed; the existing strict configuration remains. The regenerated lockfile contains neither glob nor minimatch. Fresh full npm and production-only audits both report **zero vulnerabilities**. NuGet transitive audit also reports none. These are time-specific advisory results, not a guarantee against undisclosed vulnerabilities.

New npm emits pending install-script warnings for optional native tooling (`@parcel/watcher`, esbuild, lmdb and msgpackr-extract). Warnings remain visible; clean installation, tests and production compilation succeeded on this Windows environment without blanket script approval. Other platforms still need their own verification.

## Executed final checks

Environment: Windows, .NET SDK 10.0.403/runtime 10.0.14, Node 24.18.0, npm 11.16.0, SQL Server Express 2025, Chromium.

| Command/check                                                                         | Actual result                                                                                             |
| ------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| `scripts/Verify.ps1 -RequireSql` with `TaskManagerTests__ConnectionString` configured | Exit 0                                                                                                    |
| `dotnet restore TaskManager.slnx` through helper                                      | Exit 0, nuget.org                                                                                         |
| Release build through helper                                                          | Exit 0, 0 warnings/errors                                                                                 |
| Release .NET tests through helper                                                     | **33 unit +93 integration passed; 0 failed, 0 skipped**                                                   |
| `npm ci` through helper                                                               | Exit 0; lockfile installation reproduced                                                                  |
| `npm test` through helper                                                             | **17 passed; 0 failed/skipped**                                                                           |
| `npm run build` through helper                                                        | Exit 0; production initial bundle 285.55 kB, within budgets                                               |
| `dotnet list TaskManager.slnx package --vulnerable --include-transitive`              | Exit 0; no vulnerable packages in all six projects                                                        |
| `npm audit --json` and `npm audit --omit=dev --json`                                  | Both exit 0; zero vulnerabilities                                                                         |
| Playwright using ignored isolated-port/proxy configuration                            | **6 passed; 0 failed/skipped**: three workflows each in desktop UTC and mobile America/Sao_Paulo Chromium |
| SQL-less problem/JSON/authentication test filter                                      | **53 passed; 0 failed/skipped**, not additional unique cases beyond the full suite                        |
| `Verify.ps1 -RequireSql` without SQL configuration                                    | Expected configuration exception verified before builds/tests                                             |
| Explicit Production `--migrate` against empty generated database                      | Exit 0                                                                                                    |
| SQL cleanup query                                                                     | 0 remaining generated TaskManagerTest_* databases                                                         |

The real SQL/HTTP tests verify registration/login, salted passwords, JWT rejection, public/protected endpoints, CRUD/status codes, ownership isolation, typed-parameter round trips, constraints and repeatable/concurrent seed behavior. Browser checks additionally log in with the documented demo credentials, verify all three samples/status selections, exercise persistent CRUD/filter/edit/delete/logout, and verify timestamp preservation through the UI/API/SQL.

Browser execution used generated `TaskManagerTest_Stabilization_<GUID>` SQL storage and ports 5161/4261; existing development data was not reused. Test database cleanup completed and the owned server processes were stopped. No required check remained unexecuted because of a missing service.

Raw outputs remain locally under ignored `TestResults/stabilization/`: `phase1-red.trx`, `phase1-green.trx`, `phase2-red.trx`, `phase2-frontend-red.log`, `strengthened_*.trx`, `final-verification.log`, `no-sql-pipeline.trx`, `browser-final.log`, `angular-update.log`, `npm-audit-before.json`, `npm-audit-final.json`, `npm-audit-production-final.json`, `nuget-audit-final.log`, `production-migration-final.log` and `database-cleanup-final.log`. They are optional local evidence, intentionally excluded from the final commit. This checked-in summary and the test source keep the publication understandable without those machine logs.

## Publication checks and remaining responsibility

A separate read-only Codex reviewer inspected the stabilization code and found no confirmed Critical, Important or Minor issue; it did not run tests. Pattern scans of publishable text and the one-commit history (84 historical blobs) found no candidate private keys, tokens, credential-bearing connection strings/URLs or private-feed/private-IP URLs. Git modes contained no symlinks; generated outputs are ignored. Normal Git author identity and harmless historical workspace wording do not justify rewriting history. Public demo/test passwords are intentional disposable data. A pattern scan cannot guarantee absence of every secret.

The original prompt remains unchanged. Earlier review/development evidence is preserved and clearly distinguished from current results. Documentation now uses portable repository-relative references, confirms the actual Codex/VS Code host, explains error fallback/date precision, and corrects development-key reuse across script invocations in the same shell.

Markdown verification parsed all eight documents, checked 32 local links/anchors and 11 code blocks, and found no broken reference or unclosed fence. Both prompt excerpts match the original wording, and the C# example matches the actual repository method. All seven distinct external documentation/advisory links returned HTTP 200. Prettier's Markdown formatting check passed. Local reports are `markdown-validation.json` and `external-links.json` under the ignored evidence directory.

Human code review, understanding every submitted line, interview rehearsal, license assessment and comparison with any separately supplied employer brief remain manual responsibilities. Production hosting, TLS/proxy/CSP, least-privilege SQL permissions, backups/monitoring, load testing, formal penetration testing and Safari/Firefox/physical devices were not validated. Existing scoped limitations remain: sessionStorage/XSS exposure, no token revocation/refresh, process-local rate limits, last-write-wins task updates and migration versions without checksums.

No staging, commit, push, remote creation or assessment submission was performed. Build outputs, dependency folders and verification artifacts remain locally ignored. The two obsolete planning files were already deleted by the prior authorized cleanup; original prompt and substantive development evidence remain.

Before publication, the developer should review the diff and manifest below (including the preserved analytics/IIS settings), rehearse the documented demo, and reproduce the checks on the destination environment. Only the developer should stage/commit the reviewed changes and configure/push the intended remote when authorized. No production-deployment approval is implied by readiness for a local final commit.

## Final commit manifest

The list below is generated from the final working-tree status. `M` means modified tracked file, `A` means required new file, and `D` means the existing cleanup deletion. Preserved analytics and IIS/launch-profile changes predate stabilization and are identified separately for human review.

<!-- BEGIN FINAL MANIFEST -->

| Status | File                                                                |
| ------ | ------------------------------------------------------------------- |
| M      | `docs/ARCHITECTURE.md`                                              |
| M      | `docs/DEMO.md`                                                      |
| M      | `docs/evidence/DEVELOPMENT.md`                                      |
| A      | `docs/REVIEW-2026-11-02.md`                                         |
| A      | `docs/STABILIZATION.md`                                             |
| D      | `docs/superpowers/plans/2026-11-02-taskmanager.md`                  |
| D      | `docs/superpowers/specs/2026-11-02-taskmanager-design.md`           |
| M      | `docs/VERIFICATION.md`                                              |
| M      | `GENAI.md`                                                          |
| M      | `README.md`                                                         |
| M      | `scripts/Start-DevApi.ps1`                                          |
| M      | `scripts/Verify.ps1`                                                |
| M      | `src/TaskManager.Api/ApiExceptionHandler.cs`                        |
| A      | `src/TaskManager.Api/ProblemResponses.cs`                           |
| M      | `src/TaskManager.Api/Program.cs`                                    |
| M      | `src/TaskManager.Api/Properties/launchSettings.json`                |
| A      | `src/TaskManager.Api/TaskStateJsonConverter.cs`                     |
| A      | `src/TaskManager.Api/UtcDateTimeOffsetJsonConverter.cs`             |
| M      | `taskmanager-web/angular.json`                                      |
| M      | `taskmanager-web/e2e/taskmanager.spec.ts`                           |
| M      | `taskmanager-web/package-lock.json`                                 |
| M      | `taskmanager-web/package.json`                                      |
| M      | `taskmanager-web/playwright.config.ts`                              |
| M      | `taskmanager-web/src/app/app.component.ts`                          |
| M      | `taskmanager-web/src/app/app.config.ts`                             |
| M      | `taskmanager-web/src/app/auth/auth.component.ts`                    |
| M      | `taskmanager-web/src/app/tasks/dashboard.component.ts`              |
| A      | `taskmanager-web/src/app/tasks/task-editor.component.spec.ts`       |
| M      | `taskmanager-web/src/app/tasks/task-editor.component.ts`            |
| M      | `taskmanager-web/src/app/tasks/task-form.ts`                        |
| M      | `tests/TaskManager.IntegrationTests/ApiTests.cs`                    |
| A      | `tests/TaskManager.IntegrationTests/AuthenticationPipelineTests.cs` |
| A      | `tests/TaskManager.IntegrationTests/JsonContractTests.cs`           |
| A      | `tests/TaskManager.IntegrationTests/PipelineFactory.cs`             |
| A      | `tests/TaskManager.IntegrationTests/ProblemResponseTests.cs`        |
| M      | `tests/TaskManager.IntegrationTests/RepositoryTests.cs`             |

Preserved pre-existing user changes: `src/TaskManager.Api/Properties/launchSettings.json` (IIS Express/profile formatting) and `taskmanager-web/angular.json` (analytics setting). Review these choices before committing; they were not silently overwritten. `taskmanager-web/tsconfig.app.json` has the same Git content hash as HEAD and needs no content change, even if stale index metadata temporarily lists it as modified.

Include the two deletion entries as the previously authorized cleanup. Keep the new review/stabilization reports and all added regression fixtures. Exclude ignored `TestResults/`, `node_modules/`, `bin/`, `obj/`, `dist/`, `.angular/`, `.vs/` and browser outputs. No staging command was executed.
