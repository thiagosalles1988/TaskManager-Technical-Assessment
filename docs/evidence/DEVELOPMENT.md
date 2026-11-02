# Development evidence

Date: 2026-11-02. Commands executed by Codex. Earlier raw logs/TRX were removed during requested cleanup; this factual historical summary remains. New stabilization evidence follows below, and new raw results are retained under ignored TestResults/stabilization. No personal human code review is claimed.

- Original workspace: an isolated assessment worktree preserved the unrelated original checkout; no remote operation occurred.
- Environment: .NET SDK 10.0.403/runtime 10.0.14, Node 24.18.0/npm 11.16.0, SQL Server Express 2025 17.0.1145.4. Integrated SQL authentication succeeded.
- Restore initially failed because the machine-level private Azure feed returned HTTP 401. Repository NuGet.Config clears inherited sources and uses nuget.org.
- SqlClient 7.0.2 brought a vulnerable PKCS dependency (NU1902/NU1903). Selected SqlClient 7.1.1 and pinned System.Security.Cryptography.Pkcs 10.0.14. Restore then passed with warnings as errors.
- M1 RED: 11 domain tests failed against temporary NotImplementedException methods; GREEN: 11/11 passed after invariant implementation.
- M2 RED: 4 repository scenarios failed at unimplemented initializer. GREEN: 4/4 real SQL tests plus 11/11 unit tests passed. CRUD round trips, ownership filters, unique normalized email, FK, schema repeatability and invalid-data constraints executed in generated TaskManagerTest_* databases, dropped by test cleanup. M2 failure was fixture-level; individual repository tests did not each receive a separate RED run.
- M3 RED: 22 service cases failed against temporary service methods, with 11 existing domain cases passing. NSubstitute substitutes only external repository/security boundaries. Service implementation followed that run.

No claim is made that every source line or UI detail was developed with a separate RED/GREEN cycle. Scaffolding/configuration and DTOs were written directly; important business behaviors were written test-first in batches. Temporary unimplemented methods are removed as each milestone is completed.

- M3 GREEN: 33 unit cases and 4 SQL scenarios passed.
- M4 RED: 14 HTTP cases failed against the template API (404). GREEN: all 14 passed with real JWT/password/services/SQL. Subsequent password/seed verification tests passed; these were added after their implementations and are not claimed as TDD cycles.
- M5 RED: 10 Angular auth/form cases failed against placeholder logic. GREEN: all 10 passed. The first production build exposed incompatible union-of-observable overloads in auth/editor submit handlers; typing the common request as Observable<unknown> fixed compilation.
- Initial Playwright run: 4/4 desktop/mobile cases passed. Visual screenshot review then found native selects showing Todo for saved InProgress/Completed tasks. New assertions failed on both desktop/mobile; explicit option selection made both pass, followed by 4/4 browser cases.
- Independent read-only Codex review found status selection, overlapping dashboard requests, blocked storage and concurrent sample seed races. It did not rerun verification. No confirmed backend IDOR/SQL injection/dependency direction bug was reported.
- Dashboard race RED: two request-coordination cases failed (two writes accepted; superseded GET not cancelled), while 10 existing frontend cases passed. GREEN: serialized mutations/latest-list cancellation passed both.
- Storage RED: blocked-storage construction threw SecurityError (1 failed/12 passed). GREEN: guarded storage access and memory fallback passed all 13 cases.
- Seed concurrency RED: a real SQL test held two missing-sample reads at a barrier and then failed on PK_Tasks duplicate insertion. GREEN: duplicate-key mapping and an owner-scoped re-read passed; exactly three samples remained.
- Failed status-update UI RED: selector remained Completed after rejected write, expected Todo. GREEN: retain server selection until success. Final frontend suite: 14/14.
- Added HTTP verification boundaries for null/empty bodies, null/missing title, null/numeric status and maximum title/description lengths. Added an injected unexpected-failure case verifying generic 500 ProblemDetails without diagnostic leakage. These extra boundary cases were verification tests, not independently claimed RED/GREEN cycles.
- Final .NET run: 33 unit +31 integration passed, 0 skipped, Release build 0 warnings/errors. NuGet transitive audit reported no vulnerable packages. SQL cleanup query found no remaining TaskManagerTest_* databases.
- Final npm install/build/test succeeded; production audit 0 vulnerabilities; full dev audit still reports 16 moderate minimatch-related Angular CLI advisories. No forced major upgrade or transitive override was performed.
- A final Playwright run exposed a test navigation race: email filled into the prior login form before the lazy register view mounted (registration error snapshot had empty email). Waiting for the registration heading fixed test synchronization. All four browser flows passed afterward, including no captured console/page errors in CRUD and no horizontal overflow.
- Startup helper was executed successfully. Verification helper initially stopped on npm warning stderr under Windows PowerShell 5.1; it now preserves native output and checks native exit codes. Full helper rerun succeeds.
- At the end of the initial implementation: no deployment/refresh/revocation/ETag/pagination; last-write-wins and sessionStorage tradeoffs documented. Human review, IDE attribution confirmation and publication were pending at that point. No push, submission or merge into the unrelated project occurred.
- Subsequent user request: relocate the assessment to its own TaskManager folder and initialize an independent local Git repository on main, without a remote. An SHA-256 manifest verified implementation preservation; only location/provenance documentation changed. At the new location, Release build passed with zero warnings/errors, 33 unit +31 SQL/HTTP tests passed, and 14 frontend tests plus production build passed. Assessment servers were stopped for relocation; unrelated tracked source was unchanged.

## Subsequent final stabilization

The human developer confirmed use of **OpenAI Codex inside Visual Studio Code**. This establishes tool/host attribution, not personal review or approval.

- The earlier [security review](../REVIEW-2026-11-02.md) reproduced defects despite its green 64-test backend suite. The [stabilization record](../STABILIZATION.md) retains the corrections, exact commands, results and final file list.
- HTTP error regression RED: 16 failed/10 passed; GREEN: 26 passed in Development and Production. TryWriteAsync with a direct safe JSON fallback preserves validation/status/error bodies under incompatible Accept and avoids Development diagnostic leakage.
- JSON/date/SQL regression RED: 13 failed/15 passed among 28 cases. Strict explicit-offset UTC parsing and exact status strings were implemented; all selected cases pass in the final suite. Existing datetime2(7) storage was retained.
- Frontend precision RED: 1 failed/16 passed; GREEN: all 17 pass. Title-only edits now preserve the original seconds/seven fractional digits; changing the local due field deliberately chooses minute precision.
- Added independent named SQL CHECK/FK assertions, a forged-owner UPDATE reaching SQL, SQL-independent real JWT/public/error pipeline tests, and a required-SQL verification guard. These additions are verification coverage, not separate claimed failing-first cycles.
- Official Angular update: core/compiler 22.2.4, CLI/build 22.2.5, TypeScript 6.0.3, no force/override. Preserve eager change detection and XHR behavior; retain strict diagnostics. Earlier glob 13.0.6/minimatch 10.2.6 are absent; current full and production-only npm audits report zero, as does NuGet transitive audit.
- Final helper run: restore, Release build 0 warnings/errors, 33 unit +93 integration passed/0 failed/0 skipped with real SQL, npm ci, 17 frontend tests and production build succeeded. A separate SQL-less filter passed 53 existing pipeline cases; missing required SQL configuration produced the expected guard exception.
- Browser run: 6 passed, 0 failed/skipped, using isolated SQL/API and desktop UTC/mobile America/Sao_Paulo Chromium. Verified demo credentials/three seed tasks, CRUD/logout and unchanged/changed due-date round trips. Explicit Production migration succeeded on a generated empty database; generated databases and owned test servers were cleaned up.
- Separate read-only Codex review found no confirmed remaining code issue; the reviewer did not execute tests. No human review, deployment or submission is claimed. No git add, commit, push or remote creation occurred. New raw evidence remains locally under ignored TestResults/stabilization; portable summaries are retained in documentation.

## Publication authorization

After stabilization, the developer requested creation of `TaskManager-Technical-Assessment`. Codex created the private GitHub repository and configured the local origin without committing or pushing. The developer then explicitly authorized uploading the files, making the repository public and keeping its description and documentation in English. That request supersedes the earlier restrictions on staging, committing, pushing and repository visibility. It does not establish independent human code review, production approval or submission to the interview panel.

The publication target is [thiagosalles1988/TaskManager-Technical-Assessment](https://github.com/thiagosalles1988/TaskManager-Technical-Assessment), using the independent TaskManager repository and its main branch. Application source, configuration, tests, documentation and the prior cleanup deletions are included; generated outputs, dependencies, local verification logs and secrets remain excluded by .gitignore. The original prompt and historical development evidence are retained.
