# TaskManagement API (.NET 9)

A Clean-Architecture Task Management Web API. EF Core code-first against SQL Server (LocalDB), JWT auth via ASP.NET Identity, FluentValidation, global exception handler implementing `IExceptionHandler`, idempotent POST, and a raw-SQL summary endpoint.

## Solution layout

```
TaskManagement.slnx
├── src/
│   ├── TaskManagement.Domain         # entities, enums (no dependencies)
│   ├── TaskManagement.Application    # DTOs, interfaces, services, validators
│   ├── TaskManagement.Infrastructure # EF Core, migrations, JWT, Identity
│   └── TaskManagement.API            # controllers, middleware, Program.cs
└── tests/
    └── TaskManagement.UnitTests      # xUnit + Moq + FluentAssertions
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

Swagger UI is exposed at `/swagger` in Development. The seed migration inserts **60 sample tasks** distributed across all status/priority combinations.

Open `TaskManagement.slnx` directly in Visual Studio 2022 17.13+ (or convert to `.sln` with `dotnet sln migrate`).

## Configuration

`appsettings.json`:
- `ConnectionStrings:DefaultConnection` — defaults to `(localdb)\MSSQLLocalDB`, database `TaskManagementDb`.
- `Jwt:Key` — **replace the placeholder with a 32+ character secret** before running in any non-dev environment.

## Endpoints

All `/api/tasks/*` routes require `Authorization: Bearer <jwt>`. Obtain a token via `/api/auth/register` or `/api/auth/login`.

| Method | Path | Notes |
| ------ | ---- | ----- |
| `GET`   | `/api/tasks?status=&priority=` | both filters optional |
| `GET`   | `/api/tasks/{id}` | 404 if missing |
| `POST`  | `/api/tasks` | optional `Idempotency-Key` header — see below |
| `PUT`   | `/api/tasks/{id}` | replaces the task |
| `PATCH` | `/api/tasks/delete-tasks` | body `{ "ids": [1,2,3] }`, soft-deletes |
| `GET`   | `/api/tasks/summary` | raw SQL: counts grouped by status × priority |
| `POST`  | `/api/auth/register` | returns JWT |
| `POST`  | `/api/auth/login` | returns JWT |

### Idempotency

`POST /api/tasks` accepts an optional `Idempotency-Key` header (≤128 chars). The server stores the key on the row; replaying the same key returns the original task without creating a duplicate. A **unique filtered index** (`UX_Tasks_IdempotencyKey`) enforces this at the DB layer so concurrent retries cannot race.

### Concurrency notes

The `TaskItem` entity carries a SQL Server `rowversion` column (`RowVersion`). EF includes it in `UPDATE`/`DELETE` `WHERE` clauses. Comments throughout the code mark the extension points:
- `TaskService.UpdateTaskAsync` — accept client-supplied `RowVersion` and copy it onto the loaded entity before save.
- Handle `DbUpdateConcurrencyException` to merge / retry / 409-respond.
- `SoftDeleteAsync` currently uses `ExecuteUpdateAsync` (fast, no rowversion check). Switch to a load-and-update pattern if soft-delete needs concurrency protection.

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

11 unit tests cover the `TaskService` happy paths and validation failures (`Title` required, invalid `Status`/`Priority` enums, idempotency, not-found, soft-delete count, summary delegation).
