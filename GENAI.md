# Generative AI development record

## Tool and provenance

AI coding tool: **OpenAI Codex** operating on the supplied local workspace. The assessment's requested attribution is **“OpenAI Codex inside Visual Studio Code.”** This execution verifies Codex's tool actions and local files; the IDE/window host is not independently established by the runtime. The human developer must confirm the Visual Studio Code attribution before submission. No human review, approval or decision is invented here.

## Actual prompt

The complete prompt supplied by the user is preserved verbatim in [docs/ASSESSMENT-PROMPT.txt](docs/ASSESSMENT-PROMPT.txt). It is the actual generation request, not a shortened prompt retrospectively presented as history. It starts:

> You are a Senior .NET Software Engineer, Software Architect, and Technical Lead.
>
> Your task is to build a complete, production-minded but appropriately scoped Full Stack application for a Senior .NET Developer technical interview.

Its task-management API requirement includes:

```text
GET    /api/tasks
GET    /api/tasks/{id}
POST   /api/tasks
PUT    /api/tasks/{id}
DELETE /api/tasks/{id}
```

The supplied prompt also requires .NET 10 MVC, Angular, SQL Server, Microsoft.Data.SqlClient, raw parameterized ADO.NET, Clean Architecture, NUnit/NSubstitute, TDD evidence, JWT ownership security and real verification. No alternate initial-scaffold prompt or fabricated prompt iteration is claimed. Codex used local planning/TDD/review workflow instructions and generated source via file edits and the standard dotnet/Angular CLI scaffolds.

## Representative actual generated code

From `src/TaskManager.Infrastructure/SqlTaskRepository.cs`, the implemented update operation:

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

The helpers add typed SqlParameters; no user input is concatenated into SQL. This sample is actual code from this development, not a hypothetical improved example. Formatting may differ after the solution formatter.

## Technical evaluation

The generated design separates invariants, application use cases, SQL and HTTP. Specific repositories avoid a custom persistence framework. The SQL update predicate enforces owner isolation even if an application lookup is mistaken. DTOs avoid exposing passwords/hashes or binding owner IDs. Controllers delegate business work. Cancellation and asynchronous resource disposal reach SqlClient. Review did not identify a confirmed backend IDOR, SQL injection or inverted project dependency.

The system remains deliberately small. It is not a full identity provider or operationally hardened deployment. Stateless tokens lack refresh/revocation, task updates use last-write-wins, and database scripts use version names rather than checksum validation. The human must evaluate these tradeoffs against actual deployment requirements.

## Actual process and validation

Detailed factual evidence is in [docs/evidence/DEVELOPMENT.md](docs/evidence/DEVELOPMENT.md); final executed commands/results are in [docs/VERIFICATION.md](docs/VERIFICATION.md).

Important behaviors were developed test-first in batches: 11 domain cases failed before invariants were implemented; 22 service cases failed before service implementation; 14 HTTP cases failed before endpoint/auth wiring; 10 Angular auth/form cases failed before frontend logic. The initial SQL RED occurred at the unimplemented shared schema fixture, not separately at each repository method. Scaffolding, DTOs, configuration, UI markup and later seed/password/boundary verification tests were not all produced through individual RED/GREEN cycles. This is not a claim of exhaustive test-first development.

Executed verification includes real SQL CRUD/constraints/ownership, real HTTP JWT and account/task flows, Angular unit tests, production compilation and desktop/mobile Chromium browser workflows. Mocked application tests are not represented as proof of SQL behavior. SQL tests were configured and executed locally rather than skipped. No public deployment, Safari/Firefox/physical-device test, penetration test or human acceptance session is claimed.

## Problems actually detected and corrections implemented

1. Workspace inspection found an existing unrelated Church application using Entity Framework. Codex initially created an isolated local assessment branch/worktree. At the user's subsequent explicit request, TaskManager was moved to `C:\Users\thiag\source\repos\TaskManager` and given its own local Git repository, independent of Church. The original Church checkout remains intact. No new remote repository, push or assessment submission occurred.
2. Initial NuGet package commands inherited a private Azure feed returning 401. A local NuGet.Config now clears inherited sources and uses nuget.org. No feed credentials were requested or added.
3. Restore detected NU1902/NU1903 for a transitive System.Security.Cryptography.Pkcs package. SqlClient was updated to 7.1.1 and PKCS explicitly pinned to patched 10.0.14; restore then succeeded with warnings treated as errors.
4. The Angular production compiler rejected a union of observables with incompatible overloads in auth/editor submission. The request variables now have an explicit common Observable type; production compilation passed.
5. Initial browser workflows passed, but screenshot inspection revealed InProgress/Completed cards showing Todo in their native status selectors. New desktop/mobile assertions failed on the observed Todo value. Selecting the matching option explicitly fixed both; all browser workflows subsequently passed.
6. Independent AI code review identified overlapping dashboard mutation/list requests that could restore stale data. Two new Angular tests failed (two pending writes; old GET not cancelled). Mutations now serialize and a superseded list subscription is cancelled. Regression tests pass.
7. Review identified a storage failure path that could prevent AuthService construction. A new blocked-storage test failed with SecurityError from removeItem. Storage access now fails gracefully with a memory-only session and reliable in-memory logout; the regression passes.
8. The first local key-generation shell command used a modern static API unavailable in Windows PowerShell 5.1. Startup failed key validation. The helper now uses RandomNumberGenerator.Create/GetBytes and the API was successfully started without printing a key.

9. Review found simultaneous seeded hosts could race on stable sample IDs. A deterministic SQL test synchronized two missing-task reads and failed on the duplicate insert. SqlTaskRepository now maps duplicate task keys to a specific application conflict; the seeder accepts it only after an owner-scoped re-read. The concurrent SQL test passes with exactly three samples.
10. A frontend failure-recovery test showed that a rejected status update left the native dropdown showing the unsaved value. The selector now retains server state until a successful refresh. The new test first failed with Completed instead of Todo, then passed after correction.

Review was performed by a separate Codex agent, read-only; it did not independently rerun the checks. The implementing agent ran the verification commands. Human code review remains pending. Remaining visible limitations include process-local rate limiting and Angular CLI development dependency advisories; no fake “all security issues solved” conclusion is made.

## Security considerations

- **Authentication:** salted platform password hashing; short-lived signed JWTs with issuer/audience/lifetime/algorithm/subject validation. Production signing keys are supplied externally.
- **Authorization:** Authorize on protected controllers, AllowAnonymous on public endpoints. Client guards are navigation helpers only.
- **Ownership:** verified subject passed to application services and SQL predicates on every applicable task operation. Foreign and missing IDs both return 404. Extra client UserId cannot set ownership.
- **Input validation:** domain title/description/status rules, application email/password validation and MVC JSON validation; invalid enum integers/dates fail binding. Due dates can be overdue.
- **SQL injection:** typed parameters for every value; fixed SQL, explicit mapping, no ORM. Generated test database identifiers are bounded/trusted fixture data.
- **Browser boundary:** safe interpolation, scoped token interceptor, expiry/401 clearing, no token logging. sessionStorage remains vulnerable to same-origin XSS; memory fallback loses state on reload.

## Edge cases evaluated

Blank/whitespace/overlong titles; description limits; missing/null/invalid task bodies; integer/unsupported/null status; invalid/absent/overdue due dates; duplicate normalized emails; bad credentials/missing users; expired/wrong-key/wrong-issuer/wrong-audience/invalid-subject tokens; unknown/foreign task IDs; SQL-like and literal HTML strings; FK/unique/check constraints; deleted-between-read-and-update; repeated schema/seed initialization; stale dashboard requests; blocked session storage; mobile width and delete cancellation. See tests for exact assertions rather than interpreting this as exhaustive combinatorial coverage.

## Human responsibility and pending work

The developer remains accountable for understanding and explaining the architecture, checking generated code and dependency licenses/advisories, confirming tool attribution, validating setup in their environment, reviewing security and deployment controls, rehearsing the demo and obtaining publication authorization. AI review is useful evidence, not a substitute for ownership. Do not claim human decisions or validation until you have performed them. No push or submission is authorized by this document.
