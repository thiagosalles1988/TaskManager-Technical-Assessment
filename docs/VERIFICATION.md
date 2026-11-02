# Verification report

Executed locally on **2026-11-02 (America/Sao_Paulo)**. These are observed results, not a claim of deployment readiness or exhaustive security testing. Full generated logs/TRX/browser screenshots are retained locally in ignored `.superpowers*` and `taskmanager-web/test-results`; they are intentionally not repository artifacts.

## Completed

- .NET 10 four-layer solution, MVC controllers, JWT/password adapters, auth/user/public/task routes and consistent ProblemDetails.
- Raw SqlClient repositories, owner-scoped SQL, schema constraints/indexes, transactional versioned migrations and opt-in development seed.
- Angular standalone routing, guard/interceptor, typed API services, reactive auth/task forms, dashboard/filter/status/delete confirmation and responsive CSS.
- NUnit/NSubstitute unit tests; real SQL repository/seed/concurrency tests; HTTP integration tests; Vitest auth/form/dashboard tests; Playwright desktop/mobile tests.
- README, architecture, demo, original prompt, factual GENAI record, startup/verification helpers and repository ignore rules.

## Verified

| Executed command/check | Observed result |
|---|---|
| `dotnet restore TaskManager.slnx` | Success using only nuget.org |
| `dotnet build TaskManager.slnx --configuration Release --no-restore` | Success, 0 warnings, 0 errors |
| `dotnet test TaskManager.slnx --configuration Release --no-build` | **33 unit + 31 integration passed**, 0 failed, 0 skipped |
| `dotnet list TaskManager.slnx package --vulnerable --include-transitive` | No vulnerable NuGet packages reported for all six projects |
| `npm install` | Success; 16 moderate development-tool advisories remain visible |
| `npm test` | **14 tests passed** in three files |
| `npm run build` | Production build succeeded, initial bundle approximately 273 kB, within configured budgets |
| `npm audit --omit=dev --audit-level=moderate` | 0 production vulnerabilities reported |
| `npm run test:e2e` | **4 passed**: two workflows each on desktop and mobile Chromium |
| `scripts/Start-DevApi.ps1` | Started actual API on localhost:5100 with generated ephemeral key, schema initialization and demo seed |
| `scripts/Verify.ps1 -SqlConnectionString '...'` | Full helper completed restore/build/64 .NET tests/npm install/14 frontend tests/production build after PowerShell stderr correction |
| Public API / OpenAPI | `/api/public/info` returned 200; development OpenAPI contained all required routes |
| Test database cleanup | SQL query found **0** remaining TaskManagerTest_* databases |
| Staged snapshot audit | No symlink mode entries, filesystem reparse points, forbidden package references or tracked build/dependency artifacts |
| Secret pattern scan | No private keys, cloud/GitHub tokens, JWT literals or credential-bearing password connection strings found in staged text |
| Original workspace | Church main checkout remained clean and unchanged; TaskManager was subsequently moved to its own folder and independent local repository at the user's request |

Real SQL tests used Windows integrated authentication on `.\SQLEXPRESS`, generated unique isolated databases and dropped only those databases. SQL was available; none of those tests were skipped. Most HTTP tests use real repositories/auth/JWT/SQL; one unexpected-error boundary test deliberately substitutes a throwing task repository to verify safe 500 responses.

The browser CRUD workflow registers a new account, validates forms, creates an overdue task containing literal HTML text, reloads it from SQL, changes status, filters, edits, cancels/confirms deletion, signs out and verifies guarded navigation. It asserts no captured console errors/page errors and no horizontal overflow. The second workflow signs in to the real demo account and verifies three seed cards and their actual status selections. Desktop/mobile screenshots were inspected visually. Mobile is Chromium emulation, not Safari or a real phone.

## Review and corrections

An independent Codex agent reviewed the source read-only. It confirmed no critical backend security/persistence issue and identified wrong initial native-select status, dashboard request races, blocked-storage failures and concurrent seed insertion. Corrections were verified with tests that failed first. A further failed-status-update UI case was reproduced and fixed. See DEVELOPMENT.md and GENAI.md for the precise scope of TDD and actual results.

The final browser run initially failed two registration workflows because test code filled the prior login form before the lazy registration route mounted. Error snapshots showed an empty registration email. The test now waits for the registration heading before filling; all four workflows passed afterward. This was a test synchronization correction, not evidence that registration was broken.

Windows PowerShell 5.1 also exposed two helper issues during actual execution: the initial static random-byte call was unsupported, and native npm warnings could become terminating errors when redirected. The startup helper uses the compatible RNG instance API; verification now preserves native stderr and checks exit codes. The helper is rerun after correction.

## Not verified

- Public deployment, reverse-proxy/TLS/CSP configuration, production SQL credentials/permissions, backups or operational monitoring.
- Physical mobile devices, Safari/Firefox, screen-reader audit, load/stress testing or formal penetration testing.
- Human independent code review, IDE attribution confirmation, interview rehearsal and final acceptance/submission.

## Known limitations

- 16 moderate development dependency advisories in Angular CLI's minimatch-related dependency tree; production npm audit is clean. A major CLI upgrade is npm's proposed fix and was not forced into this assessment.
- JWT sessionStorage XSS tradeoff, no refresh/revocation/email verification/reset/MFA, process-local rate limiting, no pagination, last-write-wins across tabs/devices, minute-precision UI date editing.
- Migration versions have no content checksum; add new numbered files rather than editing applied scripts.
- A pattern-based secret scan is a useful check, not a guarantee; the human must review before publication.

## Manual steps

1. Open `C:\Users\thiag\source\repos\TaskManager` and read README/GENAI. Confirm you can explain the code and correct the IDE attribution if needed.
2. Create/configure a disposable TaskManager SQL database, then start the API with `scripts/Start-DevApi.ps1` and Angular with `npm start` from `taskmanager-web`. Exact alternatives are in README.
3. Sign in using the documented development-only demo credentials and rehearse docs/DEMO.md. Rerun Verify.ps1 and browser tests in your environment before submission.
4. Review the local source, dependency advisories and secrets. Only after explicit publication authorization, configure the intended TaskManager GitHub remote and publish its independent `main` branch. No push, merge into Church main, remote repository creation or submission was performed here.
