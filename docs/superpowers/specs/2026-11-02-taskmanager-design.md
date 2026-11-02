# TaskManager design

Authority: [actual assessment prompt](../../ASSESSMENT-PROMPT.txt). The user explicitly requested autonomous implementation; no design approval pause is needed.

Build a small personal task manager for a senior .NET interview. Preserve the existing Church checkout by using the `taskmanager-assessment` branch in a linked worktree. The new branch's snapshot contains only TaskManager; existing Church history is retained. This is an implementation decision, not a user-approved change to the original application.

Subsequent user direction supersedes that workspace arrangement: TaskManager is now in `C:\Users\thiag\source\repos\TaskManager`, with an independent local Git repository on `main`. Church remains a separate project; its history and remote are not inherited by TaskManager.

Use .NET 10 MVC controllers, Domain/Application/Infrastructure/Api projects, SQL Server with Microsoft.Data.SqlClient, and standalone Angular with Router, Reactive Forms and HttpClient. No ORM, mediator, generic repository, distributed service or global state library.

TaskItem owns validation and transitions; TaskService obtains ownership only from its caller's verified identity. Every task SQL statement filters by UserId. AuthService normalizes email and delegates password hashing and token generation through narrow interfaces. API uses the platform PasswordHasher and JWT middleware. SQL unique constraints resolve concurrent registration races.

Dates are nullable UTC instants (DateTimeOffset at API boundaries, datetime2 in SQL). Overdue tasks remain valid. Statuses serialize as Todo/InProgress/Completed strings; numeric enum inputs are rejected. Update/delete return 404 for unknown or foreign IDs. Errors use ProblemDetails without exception disclosure.

Development initialization applies embedded numbered SQL migrations with an application lock and transaction. Demo seeding requires explicit Development environment and flag, hashes the documented throwaway password, and does not reset existing accounts or duplicate tasks. Production migrations are an explicit operator step.

JWT uses a configured strong signing key, issuer/audience/lifetime checks and short expiry. The frontend stores it in sessionStorage (documented XSS tradeoff), attaches it only to /api, guards routes, and clears it on expiry or 401. Same-origin deployment is preferred; Angular's development proxy avoids CORS setup. No token refresh, email verification, password reset, pagination or multi-device revocation in scope.

Alternatives considered: mixing with Church would import forbidden EF dependencies; replacing Church in the original checkout would disturb unrelated work. Separate deployed auth/task services would add deployment and distributed consistency costs without a product need. Separate controllers in one host retain clear HTTP responsibilities.

UI: clean blue/teal palette, compact header, summary counts, status filters, task cards, dedicated create/edit routes and explicit delete confirmation. Mobile layout stacks controls, uses visible labels and focus styles. No decorative dependencies.

Verification: test-first domain/service behaviors, real isolated SQL persistence tests, WebApplicationFactory HTTP tests using real JWT and SQL, Angular tests and browser smoke checks. Record real RED/GREEN results, review findings and limitations in evidence and GENAI.md.
