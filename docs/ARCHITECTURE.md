# Architecture and engineering decisions

## Dependency direction

`Domain` is a package-free class library. `Application -> Domain`; `Infrastructure -> Application -> Domain`; `Api -> Application + Infrastructure`. Infrastructure's application dependency provides repository interfaces, not HTTP behavior. API references Infrastructure only as the composition root. Angular interacts exclusively through HTTP DTOs.

`TaskItem` is immutable outside controlled factories/update methods. Title, description, status and owner invariants apply to both new and updated entities; update preserves identity, owner and creation time. UTC conversion occurs at the domain boundary. `User` is an internal immutable persistence record; registration policy belongs to AuthService. User DTOs contain only ID and email.

Controllers translate HTTP requests into application calls and select HTTP results. TaskService controls owner-scoped lookup, mutation and not-found behavior; AuthService controls registration validation, normalization, uniqueness checks and credential verification. Neither knows SQL or ASP.NET types. Narrow `IPasswordService`/`ITokenService` interfaces keep cryptographic/platform adapters out of application tests. TimeProvider allows deterministic time behavior without a custom clock framework.

## Persistence

Raw ADO.NET is an assessment requirement and makes the SQL/security boundary explicit. IUserRepository and ITaskRepository are deliberately specific interfaces. Each repository owns SQL, typed parameters and row mapping. No generic repository, unit-of-work facade, reflection mapper, ORM or micro-ORM is introduced.

Connections are opened per operation and asynchronously disposed. SqlClient pooling handles connection reuse. Cancellation flows from HTTP through services into connection/command/reader operations. A failed OpenAsync disposes the connection before rethrowing. SQL text is fixed except a constant column list; values always use explicitly typed/length-bounded SqlParameters, including nullable UTC datetime2 values. No AddWithValue type inference is used.

Read/update/delete task predicates include `Id` and authenticated `UserId`; listing includes UserId. Update changes editable fields only, never owner. A defensive entity-owner check precedes the UPDATE, and the SQL predicate remains authoritative. An affected-row count of zero means not found, covering deletion between lookup and mutation. Single SQL CRUD statements are atomic without an extra application transaction.

Database constraints reinforce policy: normalized email uniqueness, task owner FK, nonblank title and supported status. SQL nvarchar sizes match .NET UTF-16 length limits. NormalizedEmail uses explicit binary collation so the application normalization policy controls equivalence instead of an instance's default accent/case behavior. This does not implement provider-specific email aliases, Unicode spoof detection or internationalized email normalization; the scope is ordinary validated email identities.

Email availability is checked for clear behavior, then enforced by SQL for concurrent requests. SqlClient duplicate-key codes 2601/2627 map to DuplicateEmailException and HTTP 409. Other SQL errors remain unexpected server failures and are never mislabeled as client validation.

## Schema lifecycle and seed

`000-create-database.sql` creates an empty development database if absent. `schema/001-initial.sql` defines tables/indexes. Numbered schema files are embedded in Infrastructure, so migration does not depend on working directory. DatabaseInitializer acquires a transaction-owned SQL application lock, creates/reads SchemaVersions, applies pending scripts in ordinal order and records versions in the same transaction. Failure rolls everything back; other initializers cannot observe a half-applied schema.

Do not edit an applied file: versions are tracked by resource name, not content checksum. Add a new numbered file for changes. The current runner intentionally does not support SQLCMD directives or GO separators inside embedded schema files; each file is one SQL batch. Production applies migrations explicitly with a DDL identity (`--migrate`), then runs with DML-only credentials. Automatic schema/seed flags are development-only and fail closed otherwise.

Seeding is opt-in. The demo password is public, disposable and hashed using the same password adapter as registration. Existing account passwords and edited sample tasks are preserved. Missing stable sample IDs are restored. Duplicate-email races re-read the account, and duplicate sample-key races are accepted only after an owner-scoped re-read confirms the expected sample exists. A deterministic real-SQL concurrency test verifies simultaneous starts. Seeding is not a production provisioning system.

## Authentication and authorization

Authentication and tasks have separate MVC controllers in one ASP.NET host. Separate services/hosts would add deployments, network failures, shared identity configuration and distributed testing with no useful boundary for this assessment. Controllers provide a clear HTTP separation while application services remain independently testable.

The platform PasswordHasher uses Identity V3 salted PBKDF2-HMAC-SHA512, configured for 210,000 iterations. The project uses the Identity hashing primitive, not Identity's persistence stack. Password length is bounded before hashing. Unknown accounts perform dummy verification to reduce account-enumeration timing. Duplicate registration necessarily reveals an existing email through 409; signup is rate limited. A successful verification needing a future rehash is accepted; persisting upgraded hashes is future work.

JWTs are generated with the platform JWT library using HMAC-SHA256 and a random deployment-managed signing key. No custom crypto exists. The middleware restricts algorithm, requires signed/expiring tokens, validates issuer/audience/lifetime with 15-second skew, and rejects non-GUID or empty subjects. Controllers derive ownership solely from that validated `sub` claim. Request DTOs have no UserId; extra JSON fields cannot change ownership. Public endpoints use AllowAnonymous; authenticated controllers inherit Authorize.

Tokens contain only subject/JTI plus standard lifetime/issuer/audience claims. Safe account information is a separate login response DTO. Passwords, hashes and tokens are not logged. Logout clears browser state, not an already-issued token. Refresh, revocation, MFA, password reset and email verification are intentionally absent. Production could use an external OIDC provider; that would replace the small assessment credential issuer rather than add ad hoc OAuth.

## HTTP and error boundaries

Resource creation returns 201 and a Location header; full update/delete return 204. PUT is a full replacement of editable fields; missing optional description/date become empty/null and omitted status defaults to Todo. Unknown or foreign task IDs return indistinguishable 404. Invalid authentication is 401, duplicate email 409, invalid input 400. A dedicated JSON converter accepts exactly the case-sensitive strings Todo, InProgress and Completed; integers, numeric strings and combined enum names are refused.

ApiController handles JSON/model-binding failures. Domain validation becomes ValidationProblemDetails. Expected exceptions map to safe ProblemDetails; unexpected exceptions are logged server-side and produce a generic 500 with trace ID. Development and Production regression fixtures verify safe JSON responses without stack traces. Exception and status-code middleware use TryWriteAsync, falling back directly to application/problem+json when Accept is incompatible, preserving status and ValidationProblemDetails.errors without recursive negotiation. MVC binding errors also return safe problem JSON. Kestrel limits request bodies to 32 KiB. Anonymous auth endpoints share a process-local per-IP fixed-window rate limiter; a reverse proxy deployment needs trusted forwarding and a shared limiter for multiple instances.

No CORS policy is necessary for the preferred same-origin/proxy setup. SQL encryption defaults to on; local self-signed trust is restricted to development configuration. API responses set no-store, nosniff and no-referrer. Production enables HTTPS redirection/HSTS, but TLS termination, hostname allowlists, CSP, proxy trust, deployment secrets and backup/monitoring are operator responsibilities and were not deployed during this work.

## Frontend

Standalone routed components cover login/registration, dashboard and create/edit. Dedicated typed API/auth services use HttpClient; reactive forms express limits and required fields. Signals provide local component/session state; RxJS handles requests. No global store or UI framework is needed.

The guard checks session expiry for navigation; the server remains the authorization boundary. The interceptor attaches tokens only to relative API URLs, excludes auth endpoints and clears sessions on protected 401. A timer handles expiry even without a new API call. sessionStorage balances interview simplicity and per-tab persistence but is readable by injected JavaScript. Angular text interpolation escapes task content; no bypassSecurityTrustHtml/innerHTML injection is used. If storage is blocked, login can use memory until reload. A BFF with HttpOnly cookies would mitigate token exfiltration but introduces cookies/CSRF and a different authentication flow.

The API requires ISO timestamps with seconds and explicit Z or numeric offset, accepting at most seven fractional digits. A dedicated DateTimeOffset converter rejects offsetless/invalid/over-precision values, normalizes to UTC and writes canonical Z timestamps. Domain normalization, typed DateTime2 parameters and UTC reader mapping preserve SQL datetime2(7); no schema change or data rewrite is needed. Null and overdue dates remain valid.

The editor shows browser-local time to the minute. Unchanged due fields reuse the exact original server string, preserving seconds and sub-millisecond ticks. A changed field deliberately becomes a new minute-resolution UTC instant through JavaScript Date. DST-ambiguous wall times follow the browser conversion; a disambiguation picker is outside scope. Stored dates are instants, not all-day dates. Desktop UTC and mobile America/Sao_Paulo browser tests verify unchanged precision and changed local values.

Dashboard mutations are serialized, and superseded list requests are cancelled to avoid stale-response replacement. Server updates still use last-write-wins across separate tabs/devices; a SQL rowversion with If-Match/ETag is the appropriate next step when conflict prevention becomes a product need.

## Verification and alternatives

NUnit/NSubstitute tests isolate domain/application rules from slow SQL and cryptographic boundaries. Real SQL integration tests verify persistence, ownership, uniqueness, constraints and migration repetition. SQL-backed WebApplicationFactory tests exercise real services/repositories/passwords/JWT middleware. Separate pipeline fixtures substitute only persistence so public/protected access, invalid JWTs, serialization and Development/Production error tests run without SQL; no fake auth handler bypasses security. Individual SQL CHECK failures assert error 547 and the specific constraint. A forged-owner entity forces UPDATE through to its SQL ownership predicate. Verify.ps1 -RequireSql fails fast if required SQL configuration is absent. Angular Vitest tests cover session/form/request coordination. Playwright exercises real browser/Angular/API/SQL flows at desktop/mobile sizes. These are complementary, not interchangeable proofs.

No Docker fallback was needed because SQL Server Express was available. Docker is not mandatory. The solution keeps the exact required .NET 10/SQL/Angular stack; The initial Angular 21 toolchain had development-only glob/minimatch advisories. Supported upstream migrations upgraded core to 22.2.4, CLI/build to 22.2.5 and TypeScript to 6.0.3, compatible with Node 24.18.0. Eager change detection and XHR behavior are retained explicitly; strict template diagnostics remain enabled. No forced peers, audit suppression or transitive override was added. Full npm audit reports zero vulnerabilities at verification time. See verification evidence for actual execution, not inferred coverage.

References consulted: [Angular version compatibility](https://angular.dev/reference/versions), [ASP.NET Core JWT Bearer authentication](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication), [SqlClient lifecycle](https://learn.microsoft.com/sql/connect/ado-net/sqlclient-driver-support-lifecycle), and the [PKCS vulnerability advisory](https://github.com/advisories/GHSA-846f-f764-76mx) used to select a patched dependency.
