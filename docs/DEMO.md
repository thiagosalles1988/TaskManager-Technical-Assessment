# Interview demonstration

## Before the call

Open the TaskManager repository root. Follow README quick start; verify SQL and both servers. Sign in with `demo@taskmanager.local` / `Demo-TaskManager-2026!` (development only). Start once with seeding, then keep the process running so the ephemeral JWT key stays stable. Open a second private browser session with another account for ownership demonstration. Keep the test summary and these files ready: TaskItem.cs, TaskService.cs, SqlTaskRepository.cs, ApiControllers.cs, Program.cs, ApiTests.cs, GENAI.md.

Never display real secrets, bearer token values, user-secrets output or credential-bearing connection strings while presenting. The throwaway demo password is intentionally public. Human rehearsal and independent code review remain pending until you perform them.

## Five-minute live plan

| Time      | Action and explanation                                                                                                                                                                                                                                                                            |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 0:00–0:40 | Show the dashboard and scope. “This is a personal task manager. Every task belongs to the signed-in user. The goal is a small complete feature with defensible security and tests.”                                                                                                               |
| 0:40–1:25 | Create a task with title, description and a past due date. Show validation on an empty title, then save. Explain overdue tasks are legitimate. Reload to demonstrate SQL persistence.                                                                                                             |
| 1:25–2:00 | Change status, filter, edit and save. Open deletion confirmation, cancel once, then delete. Mention mobile layout and accessible form labels.                                                                                                                                                     |
| 2:00–2:35 | In a second account/session, attempt another user's task URL using a request tool without showing tokens on screen. Explain 404 prevents revealing existence; show the HTTP integration test if live tooling is inconvenient. Anonymous task requests return 401; public info remains accessible. |
| 2:35–3:35 | Trace one create/update through controller → service → domain → repository. Point to title/status validation and the SQL `WHERE Id=@Id AND UserId=@UserId`. Show typed parameters, async disposal and cancellation.                                                                               |
| 3:35–4:25 | Show NUnit and SQL/HTTP test results, then Angular/browser results. Distinguish mocks from real SQL. Explain one RED/GREEN cycle and the status-selector regression caught by screenshot review.                                                                                                  |
| 4:25–5:00 | Open GENAI.md. Explain the actual prompt, generated code review and corrections. State limits: sessionStorage XSS tradeoff, short-lived JWT without refresh/revocation, last-write-wins, local rate limiting. Close with what you would add only when requirements justify it.                    |

## Concise technical script

“I kept authentication and task APIs in separate controllers within one host. Domain owns task invariants, Application coordinates use cases through narrow interfaces, and Infrastructure owns raw ADO.NET SQL. This keeps business logic independent of MVC and SQL Server details without adding a mediator or generic repository.

“Authentication uses the ASP.NET Core bearer middleware and salted platform password hashing. The token subject becomes the owner argument; the client never chooses ownership. Every applicable task SQL query filters by that owner, and a foreign ID returns 404. SQL parameters prevent injection; database constraints protect unique emails and valid relationships.

“The frontend uses standalone Angular, reactive forms, a small auth service, a guard and an interceptor. It stores the short-lived JWT in sessionStorage for this assessment, with the XSS tradeoff documented. Server authorization remains authoritative.

“I used Codex to generate and refine the implementation, then executed builds, tests, real SQL operations and browser flows. I recorded the actual prompt and corrections instead of claiming AI output is inherently correct. As the developer, I must understand, independently review and take responsibility for every submitted line.”

## Expected review questions

| Question                                       | Answer                                                                                                                                                                                                                                                      |
| ---------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Why ADO.NET instead of EF or Dapper?           | Mandatory assessment constraint. Explicit SQL also exposes ownership predicates, parameter types and mapping for review. No home-grown ORM is needed.                                                                                                       |
| Why four projects?                             | Enforces dependency direction at compile time. Domain stays package-free; use cases remain testable without MVC or SQL.                                                                                                                                     |
| Why no MediatR/CQRS?                           | Two small services express these use cases clearly. Additional dispatch abstractions add indirection without a current need.                                                                                                                                |
| Can a user change UserId in the request?       | DTOs do not bind it. The controller passes only verified sub, and SQL independently scopes by that owner. HTTP tests include an extra client UserId.                                                                                                        |
| What about duplicate signup races?             | The pre-check helps normal flow; the SQL unique constraint is authoritative and duplicate-key exceptions become 409.                                                                                                                                        |
| Why 404 rather than 403 for foreign tasks?     | It avoids confirming that another user's resource exists and matches the assessment contract.                                                                                                                                                               |
| Why no transaction around each task operation? | One SQL statement is already atomic. Schema initialization has multiple dependent statements and uses a transaction/application lock.                                                                                                                       |
| Are hashes encrypted?                          | No. Platform PasswordHasher applies salted, one-way PBKDF2-HMAC-SHA512 with configured work factor. Passwords are never stored or returned.                                                                                                                 |
| Does logout revoke a token?                    | It clears browser state. A copied token remains valid until expiry. Revocation/refresh flows are out of scope and explicitly documented.                                                                                                                    |
| Why sessionStorage?                            | Assessment simplicity and per-tab lifetime. XSS can read it. A production BFF/HttpOnly-cookie approach would change the flow and require CSRF protection.                                                                                                   |
| Can SQL or HTML be injected through a title?   | SQL values are parameters. Angular interpolates text safely. A SQL-like string is round-tripped in repository tests; literal HTML text is exercised in browser CRUD.                                                                                        |
| What happens with concurrent edits?            | SQL ownership remains safe; editable fields use last-write-wins. Use rowversion/ETag for multi-device conflict detection when required. Dashboard requests themselves are serialized/cancelled to prevent stale UI responses.                               |
| Do mocked tests prove SQL works?               | No. Separate real SQL tests verify repository persistence/constraints/ownership, and HTTP tests use those repositories. Unconfigured SQL tests report skips.                                                                                                |
| What did GenAI get wrong?                      | Initial UI status selection and request coordination needed correction; build verification also caught an observable union typing error. Dependency restore/audits found environment/feed and vulnerable transitive-package issues. See the factual record. |
| Is this fully production deployed?             | No. It is a working locally verified assessment. Deployment TLS/proxy/CSP/secrets, operational controls and human review are separate manual responsibilities.                                                                                              |

## Publishing

The developer authorized publishing [TaskManager-Technical-Assessment](https://github.com/thiagosalles1988/TaskManager-Technical-Assessment) as a public repository. The default branch is `main`. Before sharing the assessment link with the interview panel, review the source, dependency audits and GENAI record, and rehearse the demonstration. GitHub publication does not constitute submission to the panel or independent human technical approval.
