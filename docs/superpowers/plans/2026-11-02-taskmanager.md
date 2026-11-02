# TaskManager implementation plan

Spec: [design](../specs/2026-11-02-taskmanager-design.md), [full request](../../ASSESSMENT-PROMPT.txt).
Execute inline, sequentially as requested. Keep the original Church checkout intact. Never push or submit.

Global constraints: .NET 10, MVC controllers, Angular standalone/reactive forms, SQL Server, raw Microsoft.Data.SqlClient, NUnit/NSubstitute; no EF/Dapper/Mediator or secrets in source.

- [x] M1 — Solution structure, domain, initial tests. Create four projects and two NUnit projects. Write failing invariant tests, implement User/TaskItem/TaskState and verify unit suite and dependency direction.
- [x] M2 — SQL schema and ADO.NET repositories. Define application repository contracts. Write SQL CRUD/ownership/unique-email tests against a generated database. Apply numbered schema with constraints/indexes, implement async typed parameters/reader mapping/disposal, verify real SQL tests.
- [x] M3 — Services. Write failing NUnit/NSubstitute tests for required task and auth behaviors, including duplicate races, invalid registration and ownership. Implement TaskService/AuthService with narrow security abstractions. Verify unit suite.
- [x] M4 — API/authentication. Write HTTP contract/security tests using WebApplicationFactory and real SQL/JWT. Implement thin controllers, JWT/password adapters, ProblemDetails, opt-in development seed, DI, OpenAPI and auth rate limiting. Verify API suite and seed idempotence.
- [x] M5 — Angular. Create standalone routed app with typed services, interceptor, guard, forms, dashboard, editor, status changes and delete confirmation. Write frontend behavior tests before core auth/form logic. Build and run frontend tests.
- [x] M6 — Verification and fixes. Run restore/build/test, npm install/build/test, real browser demonstration and SQL checks. Review security, HTTP semantics, ownership, disposal and dependency graph. Fix confirmed issues with regression tests.
- [x] M7 — Documentation/readiness. README, GENAI, architecture, demo, setup scripts, exact TDD evidence and final results. Audit dependencies, tracked symlinks/secrets/artifacts and preserve local work without push/submission.

Interfaces: Domain supplies validated User/TaskItem to repository contracts. Repositories accept cancellation and owner IDs; services consume those contracts. API maps authenticated subject to service owner argument and never binds client owner IDs. Angular consumes camelCase DTOs and string statuses; edit sends a full replacement and handles 204. All test databases have TaskManagerTest_ prefix and are isolated from demo/Church data.

Review focus: malformed/expired JWT; concurrent email registration; Unicode/whitespace title bounds; absent/null/malformed dates and enum integers; foreign-owner IDs on every read/write. Tests belong to domain, SQL and HTTP milestones respectively.

Expected checks: dotnet restore/build/test TaskManager.slnx succeed; npm install/build/test succeed; SQL tests execute without skips when configured. Missing external checks must be reported honestly.
