# Generative AI Development Record

## 1. Overview and AI Tool

**OpenAI Codex** generated and refined TaskManager using source edits and standard
.NET and Angular CLI scaffolds. Codex performed source inspection, automated
verification and separate AI reviews; no personal developer review is claimed.
The human developer confirmed that development used **OpenAI Codex inside Visual Studio Code**. This confirms the tool and host, not personal code review or approval.

## 2. Actual Prompt Used

The [complete original prompt](docs/ASSESSMENT-PROMPT.txt) is retained unchanged.
The following are verbatim excerpts from that single request, not separate prompt
iterations. Its opening establishes the role and objective:

```text
You are a Senior .NET Software Engineer, Software Architect, and Technical Lead.

Your task is to build a complete, production-minded but appropriately scoped Full Stack application for a Senior .NET Developer technical interview.
```

Its complete task-management API section states:

```text
7. TASK MANAGEMENT API
==================================================

Implement the following endpoints:

GET    /api/tasks
GET    /api/tasks/{id}
POST   /api/tasks
PUT    /api/tasks/{id}
DELETE /api/tasks/{id}

All task endpoints require authentication.

Use appropriate HTTP status codes:

200 - Successful retrieval
201 - Resource created
204 - Successful update or deletion
400 - Validation failure
401 - Missing or invalid authentication
404 - Resource not found or not owned
409 - Conflict when applicable
500 - Unexpected server errors

Use consistent error responses, preferably ProblemDetails.

Never expose exception stack traces in production responses.

Task validation:

- Title is required.
- Title maximum length: 120 characters.
- Description maximum length: 2000 characters.
- Status must be a supported value.
- Due date must be a valid date when provided.
- A task must always belong to a user.
- UserId must never be trusted from the client request.

Supported task statuses:
- Todo
- InProgress
- Completed

Do not add unnecessary business rules that prevent legitimate overdue tasks.

Use consistent request and response DTOs.

Return task information without exposing sensitive user data.
```

The same prompt requires .NET 10 MVC, Clean Architecture, SQL Server, raw
Microsoft.Data.SqlClient, parameterized queries, JWT authentication and
NUnit/NSubstitute testing. It prohibits ORMs, Dapper and mediator libraries.

## 3. Generated Code Example

Actual generated update method from
[SqlTaskRepository.cs](src/TaskManager.Infrastructure/SqlTaskRepository.cs):

```csharp
public async Task<bool> UpdateAsync(TaskItem task, Guid userId, CancellationToken cancellationToken)
{
    if (task.UserId != userId) return false;
    await using var connection = await connections.OpenAsync(cancellationToken);
    await using var command = new SqlCommand("""
        UPDATE dbo.Tasks SET Title=@Title,Description=@Description,Status=@Status,
            DueDateUtc=@DueDateUtc,UpdatedAtUtc=@UpdatedAtUtc
        WHERE Id=@Id AND UserId=@UserId
        """, connection);
    AddIdentity(command, task.Id, userId);
    AddFields(command, task);
    return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
}
```

`AddIdentity` and `AddFields` add typed `SqlParameter` values. The owner predicate,
affected-row check, cancellation and asynchronous disposal are explicit.

## 4. Technical Evaluation of AI Output

Codex checked the implementation against the request rather than treating generated
code or passing tests as sufficient evidence. The reviewed design separates domain
invariants, application use cases, SQL persistence and thin MVC controllers.
Specific repositories and narrow password/token interfaces keep business logic
independent of SQL and HTTP without introducing a generic persistence framework.
DTOs neither expose password hashes nor bind client-selected ownership.

The [security review](docs/REVIEW-2026-11-02.md) found no confirmed backend IDOR,
SQL injection or inverted dependency, but reproduced defects despite green suites.
The developer must critically assess those findings, test quality and deployment
tradeoffs; AI review does not establish human approval.

## 5. Issues Identified and Improvements

The [development evidence](docs/evidence/DEVELOPMENT.md) records these corrections:

| Detected issue                                                                                               | Implemented correction and evidence                                                                                                                                                                     |
| ------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Inherited private NuGet feed returned 401.                                                                   | Repository-local `NuGet.Config` uses nuget.org; restore succeeded without adding feed credentials.                                                                                                      |
| Restore flagged vulnerable transitive PKCS packages with NU1902/NU1903.                                      | SqlClient was updated to 7.1.1 and PKCS pinned to 10.0.14; restore passed with warnings treated as errors.                                                                                              |
| Domain, service and HTTP behaviors were initially absent.                                                    | Implemented invariant validation, use cases, thin controllers, JWT wiring and owner-scoped repositories after recorded failing tests.                                                                   |
| Concurrent demo seeding collided on sample IDs.                                                              | Mapped duplicate task keys to an application conflict; the seeder accepts the race only after an owner-scoped re-read confirms the sample exists.                                                       |
| Angular compilation rejected observable unions; initial selectors showed Todo for other saved states.        | Added a common observable type and explicit matching-option selection; production compilation and browser regressions passed.                                                                           |
| Overlapping dashboard requests could restore stale results; failed writes left an unsaved selection visible. | Serialized mutations, cancelled superseded reads and retained server selection until success; regression tests passed.                                                                                  |
| Blocked session storage could prevent authentication-service construction.                                   | Guarded storage access and added a memory-only fallback with reliable logout; the blocked-storage regression passed.                                                                                    |
| Windows PowerShell lacked the initial static RNG API and mishandled npm warning stderr.                      | Used `RandomNumberGenerator.Create/GetBytes` and native exit-code checks; startup and verification helpers succeeded without printing signing keys.                                                     |
| Incompatible Accept headers turned expected errors into 500s and exposed Development diagnostics.            | Added TryWriteAsync with direct safe JSON fallback; 26 Development/Production regressions verify statuses, errors and non-disclosure.                                                                   |
| Offsetless dates depended on server timezone; unrelated edits truncated seconds/ticks.                       | Required explicit offsets with at most seven fractional digits, normalized UTC output and preserved the original due string when unchanged; JSON, real SQL and browser round trips verify the contract. |
| Non-flags enum parsing accepted combined names; SQL assertions could miss individual defenses.               | Added strict status parsing, separate named CHECK assertions and a forged-owner UPDATE reaching SQL.                                                                                                    |
| Angular 21 tooling retained 16 moderate affected development packages.                                       | Applied upstream Angular 22/TypeScript 6 migrations without forced peers or overrides; clean install, tests, build and full audit succeeded with zero reported vulnerabilities.                         |

**Recorded before-and-after behavior:** a synchronized real-SQL seed test first
failed on a duplicate `PK_Tasks` insert. After race recovery was implemented, both
seed operations completed with exactly three samples. The retained
[seed tests](tests/TaskManager.IntegrationTests/SeedAndSecurityTests.cs) exercise
concurrency, repeatability and preservation of existing edits/passwords.

The seed fix has no pre-fix Git revision; its example describes recorded execution, not a reconstructed code diff. For the later stabilization, the initial Git commit and the uncommitted patch do support an actual before/after: `IProblemDetailsService.WriteAsync` failed negotiation and returned 500; `TryWriteAsync` plus a direct JSON fallback preserves 400 and validation details. The regression batch first had 16 failures/10 passes, then 26 passes. The frontend edit regression first changed `2026-11-04T15:00:45.1234567Z` to `2026-11-04T15:00:00.000Z`; it now preserves the exact original timestamp. See the [stabilization evidence](docs/STABILIZATION.md).

## 6. Testing and Validation

The [verification report](docs/VERIFICATION.md) and [stabilization evidence](docs/STABILIZATION.md) record actual Codex execution on 2026-11-02. Earlier development logs were removed during requested cleanup; their factual summaries remain. New raw logs/TRX/screenshots are retained locally under ignored `TestResults/stabilization/`, with a portable summary in the linked evidence.

| Verification                                              | Recorded result                                                                                                                           |
| --------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `dotnet restore` and Release `dotnet build`               | Passed; zero build warnings/errors.                                                                                                       |
| `dotnet test`                                             | 33 unit +93 integration tests passed; zero failed/skipped with real SQL configured.                                                       |
| `npm ci`, `npm test`, `npm run build`                     | Succeeded after the upstream update; 17 frontend tests passed.                                                                            |
| Playwright desktop/mobile Chromium                        | Six cases passed across UTC and America/Sao_Paulo: precision/local-time editing, registration/CRUD/logout and demo data.                  |
| NuGet transitive, full npm and production-only npm audits | Zero reported vulnerabilities at verification time. Earlier 16 moderate development-package findings are retained as historical evidence. |
| Explicit production migration command                     | Succeeded against an isolated SQL database.                                                                                               |

Evidence distinguishes [NUnit/NSubstitute unit tests](tests/TaskManager.UnitTests)
from [real SQL/HTTP integration tests](tests/TaskManager.IntegrationTests),
[frontend tests](taskmanager-web/src/app) and
[browser workflows](taskmanager-web/e2e/taskmanager.spec.ts). Mocked tests do not
prove SQL behavior. The reviewer inspected source and separately reproduced an
HTTP error-handling defect; Codex ran the full suites.

Recorded test-first batches were 11 domain, 22 service, 14 HTTP and 10 Angular
cases. The initial SQL RED occurred at the shared unimplemented initializer, not
independently at each repository operation. Scaffolding, DTOs, configuration, UI
markup and later seed/password/boundary checks were not all developed through
individual RED/GREEN cycles. Browser verification also caught a test navigation
race; waiting for the registration heading corrected synchronization. This is
limited TDD evidence, not a claim of exhaustive test-first development.

## 7. Authentication, Security, and Edge Cases

- **Authentication:** salted platform password hashing and bounded password input;
  dummy verification for absent accounts. JWT middleware validates signature,
  algorithm, issuer, audience, lifetime and nonempty GUID subject. Production keys
  are supplied externally.
- **Authorization and ownership:** protected controllers use `Authorize`, public
  endpoints use `AllowAnonymous`. Verified `sub` supplies the owner argument;
  applicable SQL queries independently filter by that owner. Foreign/missing task
  IDs return 404; extra client `UserId` cannot choose ownership.
- **SQL injection and validation:** typed parameters, explicit mapping and domain
  title/description/status rules; application email/password validation and MVC
  binding checks. Generated test database identifiers are guarded fixture values.
  Null/absent and overdue due dates remain valid.
- **Dates and errors:** API input requires an explicit offset and at most seven fractional digits; UTC `Z` output and SQL datetime2(7) retain ticks. Unchanged editor dates retain the exact server value. Changed local fields use minute precision and browser DST rules. Unsupported Accept falls back to safe problem JSON with the original status in Development and Production.
- **Browser boundary:** safe interpolation, scoped bearer interception and
  expiry/401/logout clearing; no explicit token logging. sessionStorage remains
  readable to same-origin XSS, and memory fallback is lost on reload.

Evaluated cases include blank/overlong fields, malformed/null bodies, invalid
numeric/null statuses and dates, duplicate normalized emails, incorrect credentials,
invalid/expired JWTs, unknown/foreign IDs, SQL-like/HTML text, database constraints,
deletion between lookup/update, repeated migrations/seeding, concurrent requests,
blocked storage, mobile width and delete cancellation. Tests and review reports
define the exact assertions; this is not exhaustive combinatorial coverage.

## 8. Limitations and Human Responsibility

The earlier review recorded three MEDIUM and four LOW findings. Their corrections and supporting regressions are recorded in the [resolution matrix](docs/REVIEW-2026-11-02.md#stabilization-resolution). A separate read-only Codex reviewer found no confirmed remaining code issue in the stabilization patch; it did not execute tests or supply human approval.

Other documented tradeoffs are short-lived JWTs without refresh/revocation,
process-local rate limiting, last-write-wins updates, version-name-only migrations
without checksums and sessionStorage exposure. This is a small assessment, not a
full identity provider or an operationally hardened deployment.

Human review, dependency/license assessment, understanding every
submitted line and interview rehearsal remain pending. Production TLS/proxy/CSP,
secrets/permissions and operational controls require validation. Public deployment,
Safari/Firefox/physical-device testing, penetration testing and human acceptance
were not performed. The developer owns the final decisions and must explain and
verify AI output before submission. No commit, push or submission is authorized
by this historical stabilization record. Subsequent explicit authorization to commit, push and publish the repository is recorded in the [development evidence](docs/evidence/DEVELOPMENT.md#publication-authorization); it does not establish personal human code review.
