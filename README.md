# TaskManagement API (.NET 9)

A Clean-Architecture Task Management Web API. EF Core code-first against SQL Server (LocalDB), JWT auth via ASP.NET Identity with account lockout, FluentValidation, global exception handler implementing `IExceptionHandler`, idempotent POST, raw-SQL summary endpoint, OutputCache + `IMemoryCache`, rate limiting, hardening headers, and Kubernetes-friendly health endpoints.

## Solution layout

```
TaskManagement.slnx
├── src/
│   ├── TaskManagement.Domain          # entities, enums (no dependencies)
│   ├── TaskManagement.Application     # DTOs, interfaces, services, validators
│   ├── TaskManagement.Infrastructure  # EF Core, migrations, JWT, Identity
│   └── TaskManagement.API             # controllers, middleware, Program.cs
└── tests/
    ├── TaskManagement.UnitTests       # xUnit + Moq + FluentAssertions
    └── TaskManagement.IntegrationTests # WebApplicationFactory + InMemory DB
```

Dependency direction: `API → Application → Domain`, `API → Infrastructure → Application → Domain`. Domain has no outward references.

## Running the API

```bash
# 1. Restore + build
dotnet build TaskManagement.slnx

# 2. Apply EF migrations (also done automatically in Development on first run)
dotnet ef database update \
  --project src/TaskManagement.Infrastructure \
  --startup-project src/TaskManagement.API

# 3. Run
dotnet run --project src/TaskManagement.API
```

Open `TaskManagement.slnx` directly in Visual Studio 2022 17.13+.

- Default URL (HTTPS): `https://localhost:7136`
- Swagger UI (Development only): `https://localhost:7136/swagger`
- Seed migration inserts **60 sample tasks** across all status/priority combos.

## Configuration

### `appsettings.json` — committed to source

| Key | Purpose |
| --- | ------- |
| `ConnectionStrings:DefaultConnection` | `(localdb)\MSSQLLocalDB`, database `TaskManagementDb` |
| `Jwt:Issuer` / `Jwt:Audience` / `Jwt:ExpiryMinutes` | JWT claim values |
| `Jwt:Key` | **EMPTY** — must come from a secret store (see below) |
| `FeatureFlags:InfoLoggingEnabled` | `true` keeps app-level info logs; `false` filters every `TaskManagement.*` logger to `Warning+` at startup |
| `RateLimit:Auth:PermitLimit` | default `5` requests/min per IP |
| `RateLimit:Tasks:PermitLimit` | default `100` requests/min per user |

### `appsettings.Development.json` — local-only overlay

Holds a dev `Jwt:Key`. **Never put a real prod key here.**

### Providing the JWT signing key in production

Startup refuses to boot if `Jwt:Key` is missing or shorter than 32 bytes. Provide it via:

```bash
# Option A — user-secrets (recommended for local dev)
cd src/TaskManagement.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "$(openssl rand -hex 32)"

# Option B — environment variable (CI/prod)
export Jwt__Key="<32+ byte secret>"

# Option C — secrets vault (Key Vault, AWS Secrets Manager, etc.)
```

## Endpoints

| Method | Path | Auth | Rate-limit | Cache | Notes |
| ------ | ---- | ---- | ---------- | ----- | ----- |
| `POST` | `/api/auth/register` | – | `auth` | – | returns JWT |
| `POST` | `/api/auth/login` | – | `auth` | – | returns JWT; 401 on bad creds, 423 if locked |
| `GET`  | `/api/tasks?status=&priority=` | JWT | `tasks` | OutputCache 30s | both filters optional |
| `GET`  | `/api/tasks/{id}` | JWT | `tasks` | – | 404 if missing |
| `POST` | `/api/tasks` | JWT | `tasks` | evicts | optional `Idempotency-Key` header |
| `PUT`  | `/api/tasks/{id}` | JWT | `tasks` | evicts | replaces the task |
| `PATCH`| `/api/tasks/delete-tasks` | JWT | `tasks` | evicts | body `{ "ids": [1,2,3] }`, soft-deletes |
| `GET`  | `/api/tasks/summary` | JWT | `tasks` | OutputCache 60s + IMemoryCache 5min | raw SQL grouped counts |
| `GET`  | `/health/live` | – | – | – | liveness probe (self check) |
| `GET`  | `/health/ready` | – | – | – | readiness probe (DB check) |

### Idempotency

`POST /api/tasks` accepts an optional `Idempotency-Key` header (≤128 chars). The server stores the key on the row; replaying the same key returns the original task without creating a duplicate. A **unique filtered index** (`UX_Tasks_IdempotencyKey`) enforces this at the DB layer so concurrent retries cannot race.

### Concurrency notes

The `TaskItem` entity carries a SQL Server `rowversion` column (`RowVersion`). EF includes it in `UPDATE`/`DELETE` `WHERE` clauses. Comments throughout the code mark the extension points:
- `TaskService.UpdateTaskAsync` — accept client-supplied `RowVersion` and copy it onto the loaded entity before save.
- Handle `DbUpdateConcurrencyException` to merge / retry / 409-respond.
- `SoftDeleteAsync` currently uses `ExecuteUpdateAsync` (fast, no rowversion check). Switch to a load-and-update pattern if soft-delete needs concurrency protection.

## Caching

| Layer | Where | TTL | Eviction |
| ----- | ----- | --- | -------- |
| `IMemoryCache` | `TaskRepository.GetSummaryAsync` | 5 min absolute / 1 min sliding | Repository clears on every successful `SaveChangesAsync` / `SoftDeleteAsync` |
| `OutputCache` policy `TasksList` | `[OutputCache(PolicyName="TasksList")]` on `GET /api/tasks` | 30 s, varies by `status`/`priority`/`Authorization` | Tagged `tasks`; evicted by controller after any write |
| `OutputCache` policy `TasksSummary` | `[OutputCache(PolicyName="TasksSummary")]` on `GET /api/tasks/summary` | 60 s | Tagged `tasks` / `summary` |

`OutputCache` uses an in-process store by default. Behind a load balancer, swap for `AddStackExchangeRedisOutputCache` and `IDistributedCache`.

## Security

| Control | Where |
| ------- | ----- |
| **JWT auth** + 32-byte minimum key, `ClockSkew = 0`, `RequireExpiration`, `RequireSignedTokens`, `RequireHttpsMetadata` | `Infrastructure/DependencyInjection.cs` |
| **Password policy** — 8+ chars, upper + lower + digit + symbol, ≥4 unique chars | `Infrastructure/DependencyInjection.cs` (`IdentityOptions`) |
| **Account lockout** — 5 failed logins → 15-min lock; `IdentityService` calls `AccessFailedAsync` / `IsLockedOutAsync` / `ResetAccessFailedCountAsync` | `Infrastructure/Auth/IdentityService.cs` |
| **Neutral auth errors** — same `"Invalid credentials."` for missing-user vs bad-password (no enumeration) | `Infrastructure/Auth/IdentityService.cs` |
| **Rate limiting** — `auth` policy (5/min/IP, fixed window) + `tasks` policy (100/min/user, sliding window) → 429 | `Program.cs` |
| **HSTS** (60-day max-age, includeSubDomains) in non-Development | `Program.cs` |
| **Security headers** — `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Permissions-Policy`, `Content-Security-Policy: default-src 'none'`, removes `Server` | `API/Middleware/SecurityHeadersMiddleware.cs` |
| **Tight CORS** — explicit `Authorization`/`Content-Type`/`Idempotency-Key` headers and method allow-list | `Program.cs` |
| **CSRF-safe** — JWT in `Authorization` header, no cookie auth | n/a |
| **SQL injection-safe** — raw summary query is static; EF parameterises everything else | `Infrastructure/Persistence/TaskRepository.cs` |
| **ProblemDetails responses** that don't leak stack traces | `API/Middleware/GlobalExceptionHandler.cs` |
| **Forwarded-headers** middleware so the rate limiter sees the real client IP behind proxies | `Program.cs` |

### Exception → status code mapping

| Exception | Status |
| --------- | ------ |
| `ValidationException` | 400 (ValidationProblemDetails with field errors) |
| `UnauthorizedException` | 401 |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `LockedOutException` | 423 |
| anything else | 500 (generic message, full stack logged) |

## Logging

Structured info logs at every controller, service, repository, and identity boundary using `ILogger<T>`. Sensitive payloads (passwords, tokens) are never logged.

**Feature flag** in `appsettings.json` → `FeatureFlags:InfoLoggingEnabled`. When false, `Program.cs` adds a startup filter that drops every `TaskManagement.*` logger to `Warning+` — no code change needed. Framework logs remain controlled by the standard `Logging:LogLevel` section.

Override at runtime: `FeatureFlags__InfoLoggingEnabled=false`.

## Health checks

| Endpoint | Probe | Checks | Status |
| -------- | ----- | ------ | ------ |
| `GET /health/live` | livenessProbe | `self` (process is up) | 200 / 503 |
| `GET /health/ready` | readinessProbe | `db` (`AddDbContextCheck<AppDbContext>`) | 200 / 503 |

Both endpoints are anonymous and excluded from the rate limiter. Response shape (custom writer):

```json
{
  "status": "Healthy",
  "totalDurationMs": 4.2,
  "results": [
    { "name": "db", "status": "Healthy", "durationMs": 3.1, "description": null, "error": null, "tags": ["ready"] }
  ]
}
```

Kubernetes snippet:

```yaml
livenessProbe:
  httpGet: { path: /health/live, port: 80 }
  initialDelaySeconds: 10
  periodSeconds: 30
readinessProbe:
  httpGet: { path: /health/ready, port: 80 }
  initialDelaySeconds: 5
  periodSeconds: 10
  failureThreshold: 3
```

## Indexes on `Tasks`

| Index | Purpose |
| ----- | ------- |
| `UX_Tasks_IdempotencyKey` (unique, filtered `IS NOT NULL`) | POST idempotency |
| `IX_Tasks_Status` | filter by status |
| `IX_Tasks_Priority` | filter by priority |
| `IX_Tasks_Status_Priority` | combined filter / summary |
| `IX_Tasks_AssignedTo` | filter by assignee |
| `IX_Tasks_IsDeleted` | soft-delete filter |

## Tests

```bash
dotnet test TaskManagement.slnx
```

**43 unit tests + 18 integration tests = 61 passing.**

### Unit tests (`TaskManagement.UnitTests`)

| Area | File |
| ---- | ---- |
| `TaskService` — happy paths, validation, idempotency, not-found, soft-delete count, summary delegation | `Services/TaskServiceTests.cs` |
| Validators — Create / Update / Delete / Register / Login DTOs | `Validators/ValidatorTests.cs` |
| `TaskMapper` — `ToDto`, `ToEntity`, `Apply` preserves `Id` + `IdempotencyKey` | `Mapping/TaskMapperTests.cs` |
| `GlobalExceptionHandler` — all six exception-to-status mappings | `Middleware/GlobalExceptionHandlerTests.cs` |
| `IdentityService` — register success/failure, login bad-creds, lockout transitions, success resets failure counter | `Auth/IdentityServiceTests.cs` |

### Integration tests (`TaskManagement.IntegrationTests`)

`WebApplicationFactory<Program>` boots the real `Program.cs` in-process with SQL Server swapped for the EF InMemory provider (isolated internal service provider) and the raw-SQL repository swapped for a LINQ-based stub. JWT key, feature flags, and rate limits are overridden via `UseSetting`.

| Suite | Coverage |
| ----- | -------- |
| `AuthEndpointsTests` | register → login JWT flow, weak password 400, bad creds 401, missing token 401, security headers present |
| `TasksEndpointsTests` | empty list, create + get + list, `Idempotency-Key` replay, invalid title 400, unknown id 404, update, server-side filtering, bulk soft delete, summary |
| `HealthEndpointsTests` | `/health/live` anonymous + reports `self`; `/health/ready` reports `db`; tag filtering works |
| `RateLimitingTests` | dedicated factory with `PermitLimit=2` proves the auth limiter returns 429 after a small burst |
