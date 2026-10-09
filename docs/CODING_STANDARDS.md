# ITACR Coding Standards: itacr-backend

These standards apply to the backend (.NET API and database). The frontend has its own standards for Angular in `itacr-frontend/docs/CODING_STANDARDS.md`; both share the same principles, commit convention, and language policy. Change this file through a Pull Request reviewed by the other team member.

Tooling enforces what it can: nullable reference types, .NET analyzers, the code style rules in `.editorconfig` (checked in the build; warnings are errors in Release), `dotnet format`, the commit-msg hook, and CI. Everything else is checked in code review.

## Table of contents

1. [General](#1-general)
2. [ASP.NET Core](#2-aspnet-core)
3. [Database (Azure SQL + DbUp)](#3-database-azure-sql--dbup)
4. [Language policy](#4-language-policy)

---

## 1. General

### 1.1 Principles

- Follow **Clean Code** and **SOLID**. Apply **DRY** (don't repeat yourself) and **KISS** (keep it simple): the simplest solution that meets the acceptance criteria wins.
- Write small, single-purpose methods. If a method needs "and" to describe it, split it.
- Prefer **early returns** over nested `if`/`else`.
- No dead code and no commented-out code. Git keeps the history.
- No magic numbers or strings. Use named constants or enums.

```csharp
// Bad
if (pin.Length != 4) { ... }

// Good
private const int PinLength = 4;
if (pin.Length != PinLength) { ... }
```

### 1.2 Type safety

- **Nullable reference types** are on in every project (`Directory.Build.props`). Do not disable them, and do not silence them with `!` unless a comment explains why the value cannot be null.
- Never use `dynamic` or `object` to dodge the type system.
- Mark classes `sealed` unless they are designed for inheritance. Prefer `readonly` fields and `init`-only or get-only properties.
- Use `record` types for DTOs (requests and responses): immutable and with value equality.
- Use `enum` for closed sets of values (order status, roles). Store them in the database as text (see 3.1).
- Every `async` method returns `Task` or `Task<T>` (never `async void`), is named with the `Async` suffix, and accepts and forwards a `CancellationToken`.

```csharp
public enum OrderStatus
{
    Pending,
    InKitchen,
    Ready,
    Delivered,
    Cancelled,
}

public sealed record CreateOrderRequest(Guid ClientId, IReadOnlyList<CreateOrderItemRequest> Items);
```

### 1.3 Naming

| Element                       | Convention                                    | Example                                         |
| ----------------------------- | --------------------------------------------- | ----------------------------------------------- |
| Local variables, parameters   | camelCase                                     | `totalAmount`, `cancellationToken`              |
| Methods                       | PascalCase (`Async` suffix when async)        | `CalculateTotal()`, `CreateOrderAsync()`        |
| Classes, records, enums       | PascalCase                                    | `OrdersService`, `CreateOrderRequest`           |
| Interfaces                    | `I` + PascalCase                              | `IOrdersService`                                |
| Properties                    | PascalCase                                    | `CreatedAt`, `IsPaid`                           |
| Private fields                | `_camelCase`                                  | `_connection`, `_timeProvider`                  |
| Constants, static readonly    | PascalCase                                    | `MaxItemsPerOrder`                              |
| Files                         | PascalCase, one type per file, named like the type | `OrdersService.cs`                         |
| Tables and columns            | PascalCase, tables plural                     | `dbo.Orders`, `CreatedAt`                       |
| T-SQL objects and their files | prefix + PascalCase                           | `usp_CloseCashRegister`, `fn_GetOrdersByStatus` |

- Use descriptive English names. No abbreviations except widely known ones: `Id`, `Url`, `Dto`, `Api`, `Http`, `Uuid`, `Jwt`.
- Booleans read as questions: `IsOffline`, `HasPendingOrders`, `CanCancel`.
- Type names describe the role: `OrdersController`, `OrdersService`, `CreateOrderRequest`, `OrderResponse`, `OrderRepository` (Dapper), `KitchenHub` (SignalR), `JwtOptions` (settings), `OrdersServiceTests`.

### 1.4 Formatting

- **`.editorconfig` owns formatting and code style**, applied by `dotnet format`: 4-space indentation in C# (2 in JSON, YAML, and XML), file-scoped namespaces, braces on every block, `using` directives outside the namespace, LF line endings.
- The pre-commit hook formats staged C# files, and CI runs `dotnet format --verify-no-changes`.
- **Do not debate formatting in code reviews.** If `dotnet format` accepts it, it is correct.

### 1.5 Comments and documentation

- Comments explain **why**, not **what**. If the code needs a comment to explain what it does, rename or refactor it first.
- Use **XML doc comments** (`/// <summary>`) on public APIs that are not self-explanatory. Comments on controller actions and DTOs also appear in the OpenAPI document.
- `TODO` comments must reference a Jira key: `// TODO(ITACR-15): retry with exponential backoff`.

### 1.6 Error handling

- Never swallow exceptions (`catch {}` with nothing inside is forbidden). Handle them, rethrow them, or log them with context.
- Never show stack traces, SQL errors, or internal messages to users. Return a Problem Details response with a friendly message in Spanish and log the details.
- Log errors with context (operation, relevant ids), never with secrets or PINs.
- Catch only the exceptions you can handle. Let unexpected ones reach the global exception handler.

```csharp
try
{
    var connection = await session.GetOpenConnectionAsync(cancellationToken);
    await connection.ExecuteAsync(session.Procedure("dbo.usp_Order_Create", parameters, cancellationToken));
}
catch (SqlException exception) when (exception.Number is UniqueKeyViolation or UniqueIndexViolation)
{
    // The same client UUID was synced twice: the repository reports it, and the use case returns the existing order.
    _logger.LogInformation("Order {ClientOrderId} already exists", order.ClientOrderId);
    return CreateOrderOutcome.AlreadyExists;
}
```

`UniqueKeyViolation` (`2627`) and `UniqueIndexViolation` (`2601`) are named constants for the SQL Server error numbers. `SqlException` is only caught in `Infrastructure`; `Application` never sees it.

### 1.7 Security (OWASP Top 10 basics)

- **Never commit secrets.** Locally, `.env` (Docker Compose) and `dotnet user-secrets` (API). In Azure, App Service configuration. In GitHub Actions, repository secrets. Keep `.env.example` and `appsettings.json` updated with every setting and a safe placeholder or empty value.
- Validate and sanitize **all** input. Frontend validation is only for user experience.
- Hash PINs and passwords with a slow, salted algorithm: `PasswordHasher<T>` from ASP.NET Core Identity (PBKDF2). Never store or log them in plain text. A 4-digit PIN is easy to guess, so rate-limit and lock out repeated failed logins.
- Apply least privilege: every endpoint checks the role (waiter, kitchen, manager, customer). The production database user has only the permissions the API needs.
- Keep dependencies updated: review Dependabot PRs every week and run `dotnet list package --vulnerable --include-transitive` before each sprint close.

### 1.8 Testing

- Unit tests for services and business rules (totals, order status transitions, sync logic, validation). API tests with `WebApplicationFactory<Program>` for routing, validation, authorization, and error responses.
- Test names: `<Method>_Should<ExpectedBehavior>_When<Condition>`.
- Structure every test as **Arrange, Act, Assert**.
- Write at least one test per acceptance criterion when feasible.
- Tests must be deterministic: no real network, no real dates (inject `TimeProvider` and use `FakeTimeProvider`), no order dependency between tests.

```csharp
[Fact]
public void Validate_ShouldRejectPin_WhenItHasFewerThanFourDigits()
{
    // Arrange
    var pin = "123";

    // Act
    var result = PinValidator.Validate(pin);

    // Assert
    Assert.False(result.IsValid);
}
```

---

## 2. ASP.NET Core

### 2.1 Structure

The solution follows Clean Architecture: four projects, with references pointing inward only.

```
src/
├── Itacr.Domain/             # Entities and enums per module (Identity/, Catalog/, Orders/, Customers/, Notifications/). No dependencies
├── Itacr.Application/        # Services, their interfaces, requests, and responses per module (Identity/, Catalog/)
│   └── Common/Persistence/   # IDbSession; repository interfaces live in each module (IEmployeeRepository)
├── Itacr.Infrastructure/     # Persistence/ (DbSession, <Module>/ repositories with Dapper, health check), Identity/ (JWT)
└── Itacr.Api/                # Endpoints/<Module>/ (controllers), Common/ (pipeline, options, security headers), Program.cs
```

| Project | References | Contains |
| ------- | ---------- | -------- |
| `Itacr.Domain` | nothing | Entities, enums, and rules that need no I/O |
| `Itacr.Application` | `Domain` | Business logic and the interfaces it needs (`ITokenService`, `IDbSession`, `IEmployeeRepository`) |
| `Itacr.Infrastructure` | `Application`, `Domain` | Implementations of those interfaces: Dapper repositories, JWT |
| `Itacr.Api` | `Application`, `Infrastructure` | HTTP only: routing, validation, status codes, and DI composition |

- A module has the same name in every project (`Domain/Catalog`, `Application/Catalog`, `Api/Endpoints/Catalog`), so a change to one module touches folders with the same name.
- `Domain` never uses Dapper, SQL Server, ASP.NET Core, or any other package. `Application` never references `Infrastructure` or `Api`, and never sees SQL, connections, or `SqlException`.
- A module never calls another module's controller. Shared logic moves to `Domain/` or is exposed through a service interface.
- **Controllers stay thin**: they receive a validated request, call one service method, and map the result to an HTTP response.
- Business logic lives in `Application` services, registered in DI behind an interface (`ICatalogService`). Data access goes through the repository interfaces of the module, implemented in `Infrastructure/Persistence/<Module>/`. Do not add a generic repository.
- Keep `Program.cs` short: each project registers its services in one extension method (`AddInfrastructure`, `AddApplication`), and the API adds its own (`AddApiBase`, `AddAuthFeature`).
- Inject dependencies through the constructor (primary constructors are fine). Never use `new` for services, and never resolve services manually from `IServiceProvider`.

### 2.2 Validation and configuration

- A request DTO (`record`) for every input (body, query, route). Validate it with **data annotations** (`[Required]`, `[Range]`, `[StringLength]`, `[RegularExpression]`), with Spanish `ErrorMessage` values.
- `[ApiController]` on every controller: invalid input returns `400` with `ValidationProblemDetails` automatically.
- Never bind entities directly from a request, and never return entities: map to response DTOs. This prevents over-posting and leaking columns.
- Every settings section has an **options class** bound and validated at startup, so the API fails fast when a value is missing:

```csharp
builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### 2.3 REST conventions

- Every route starts with `/api/v1`: `[Route("api/v1/orders")]` on the controller. Lowercase URLs (`options.LowercaseUrls = true`).
- Plural nouns: `/orders`, `/products`, `/orders/{id}/items`. Actions that are not CRUD use a sub-resource: `PATCH /orders/{id}/status`.
- Correct methods and status codes: `200` OK, `201` Created (with `CreatedAtAction`), `204` No Content, `400` validation, `401` unauthenticated, `403` forbidden, `404` not found, `409` conflict, `422` business rule violation, `429` rate limited.
- Errors use **RFC 9457 Problem Details** (`application/problem+json`), produced by `AddProblemDetails()`, `UseExceptionHandler()`, and `UseStatusCodePages()`. Business errors return `Problem(...)` with a Spanish `detail`:

```json
{
  "type": "https://itacr.app/problems/validation-error",
  "title": "Validation failed",
  "status": 400,
  "detail": "El PIN debe tener exactamente 4 dígitos.",
  "instance": "/api/v1/auth/pin",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

- Paginate every list: `?page=1&limit=20`, and return `{ data, meta: { page, limit, total } }`.
- JSON uses camelCase property names (the ASP.NET Core default) and enums as strings (`JsonStringEnumConverter`).
- Dates in **ISO 8601 UTC** (`2026-09-28T18:30:00.000Z`): store and return `DateTime` values with `DateTimeKind.Utc`, taken from the injected `TimeProvider`. The frontend converts to local time.
- Order creation accepts the client UUID as an **idempotency key**. Repeating the request with the same key returns the original order (`200`) instead of creating a duplicate. The unique index on `ClientId` guarantees it even when two requests race.
- Every endpoint accepts a `CancellationToken` and passes it down to the repositories and Dapper.

### 2.4 Security and operations

- Security headers on every response (`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`), HSTS and HTTPS redirection outside Development.
- Strict CORS: only the frontend origins from `Cors:Origins` (the local dev server, and in production the frontend App Service, set with the `Cors__Origins__0` environment variable). Never `AllowAnyOrigin` together with credentials.
- Rate limiting with the built-in `AddRateLimiter` (a global limiter plus a stricter policy on the PIN login endpoint).
- JWT bearer authentication with role-based authorization: `[Authorize(Roles = nameof(EmployeeRole.Manager))]`. The token lasts one shift (`Jwt:TokenLifetimeHours`, 12) and carries the `sub` (employee id), `name`, and `role` claims; there is no refresh token. Controllers are closed by default through a global `AuthorizeFilter`; public endpoints use `[AllowAnonymous]` explicitly, and SignalR hubs need `[Authorize]` on the hub class.
- PIN login is locked per employee after `LoginSecurity:MaxFailedPinAttempts` (5) wrong PINs for `LoginSecurity:LockoutMinutes` (15), and the login endpoints have their own stricter rate limit (`LoginSecurity:PermitLimit` per minute and IP) on top of the global one.
- Structured logging with message templates (`_logger.LogInformation("Order {OrderId} created", order.Id)`), never string interpolation in log calls. JSON console logs outside Development. Every log and Problem Details response includes the trace id.
- `GET /api/v1/health` checks the API and the database by running `dbo.usp_Health_Ping` (`DatabaseHealthCheck`), so it also proves the `EXECUTE` permission. It is anonymous and returns no internal details.
- Document every endpoint in OpenAPI: `[ProducesResponseType]` for each status code, XML comments on actions and DTOs. The Scalar UI (`/scalar/v1`) is enabled in Development only.
- Real time with **SignalR**: one hub per use case (`KitchenHub` at `/hubs/kitchen`), authenticated with the same JWT, and clients grouped by role or order. Hubs only push events; changes still go through the REST API. Azure App Service must have WebSockets enabled.

---

## 3. Database (Azure SQL + DbUp)

Production uses **Azure SQL Database**; local development uses **SQL Server 2025** in Docker (`compose.yaml`), the same engine generation. The database lives in `itacr-backend/database/` and reaches every environment only through `Itacr.Database.Migrator` (DbUp). The full workflow is in [database/README.md](../database/README.md).

```
database/
├── migrations/              # V001__initial_schema.sql, V002__...: schema and reference data, each runs once, in order
├── functions/<Module>/      # fn_*.sql   ┐
├── views/<Module>/          # vw_*.sql   │ CREATE OR ALTER, applied again on every run
├── procedures/<Module>/     # usp_*.sql  │
├── triggers/<Module>/       # trg_*.sql  ┘
├── scripts/                 # Run by hand only (database users, reports); never applied automatically
└── Itacr.Database.Migrator/ # DbUp console app and the employee seed
src/Itacr.Infrastructure/Persistence/
├── DbSession.cs             # One connection per request, opened on first use; transactions on demand
├── <Module>/                # Repositories with Dapper: EmployeeRepository, CatalogRepository, ...
└── DatabaseHealthCheck.cs
```

**No manual changes in the database.** Every change to tables or T-SQL objects is a script in `database/` applied by the migrator, so it is versioned and applied in the same order on every machine and environment.

### 3.1 Tables

- Tables are **PascalCase plural** in `dbo` (`dbo.Orders`), columns **PascalCase** (`CreatedAt`). The entity in `Domain/<Module>/` is PascalCase singular (`Order`) and its properties have the column names, so Dapper maps rows to it by name.
- Every schema change is a new script in `database/migrations/`, named `V<NNN>__<snake_case_description>.sql` (`V002__add_order_status_history.sql`). **Never edit or rename a script that was already merged** into `sprint-XX`, `develop`, `release`, or `main`: the journal (`dbo.SchemaVersions`) stores its name, so it would not run again. Create a new one.
- Keep schema changes additive (new tables, nullable or defaulted columns): the previous API runs for a moment against the new schema during a deploy. Rename or drop columns in a later release.
- Seeds are **idempotent**: reference data with fixed IDs goes in a migration; employees are seeded by the migrator, which checks existing emails first, so running it twice does not duplicate data.

```sql
CREATE TABLE dbo.OrderStatusHistory (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_OrderStatusHistory PRIMARY KEY,
    OrderId uniqueidentifier NOT NULL CONSTRAINT FK_OrderStatusHistory_Orders_OrderId REFERENCES dbo.Orders (Id) ON DELETE CASCADE,
    Status nvarchar(20) NOT NULL,
    ChangedAt datetime2 NOT NULL,
    CreatedAt datetime2 NOT NULL,
    UpdatedAt datetime2 NOT NULL
);

CREATE INDEX IX_OrderStatusHistory_OrderId ON dbo.OrderStatusHistory (OrderId);
```

- Name constraints and indexes like the existing ones: `PK_<Table>`, `FK_<Table>_<Referenced>_<Column>`, `IX_<Table>_<Columns>`.
- Every table has `CreatedAt` and `UpdatedAt` (`datetime2`, UTC). The code that writes the row takes the time from `TimeProvider` and passes it as `@Now`; procedures never call `SYSUTCDATETIME()`, so tests can control the clock.
- Use foreign keys, and add indexes on frequently filtered columns (`Status`, `CreatedAt`, `WaiterId`). Set a maximum length on every string; never leave `nvarchar(max)` by accident.
- Enums are stored as text (`nvarchar`), so T-SQL objects can compare them as strings. C# passes `role.ToString()`, and Dapper parses the text back into the enum.
- Combo customizations are a JSON column, stored in the native `json` type of Azure SQL and SQL Server 2025.
- Money is `decimal(10,2)` in SQL and `decimal` in C#, never `float` or `double`.
- Primary keys are `uniqueidentifier`, generated in the repository with `SequentialGuid.Create()`, whose values SQL Server sorts in creation order (no index fragmentation). Do not use `Guid.NewGuid()` for keys. Keys that come from the client (the order's `ClientOrderId`) are a separate column with a unique index.

### 3.2 T-SQL objects: procedures, functions, views, and triggers

- **One object per file** in `database/<type>/<Module>/`, named exactly like the object: `database/functions/Orders/fn_GetOrdersByStatus.sql`. Always in the `dbo` schema, always schema-qualified (`dbo.Orders`, `dbo.fn_GetOrdersByStatus`).
- **Naming**: prefix by type + PascalCase. Parameters are `@PascalCase`.

  | Object                 | Prefix | Folder                          | Example                     |
  | ---------------------- | ------ | ------------------------------- | --------------------------- |
  | Stored procedure       | `usp_` | `database/procedures/<Module>/` | `usp_CloseCashRegister`     |
  | Function               | `fn_`  | `database/functions/<Module>/`  | `fn_GetOrdersByStatus`      |
  | View                   | `vw_`  | `database/views/<Module>/`      | `vw_DailySales`             |
  | Trigger                | `trg_` | `database/triggers/<Module>/`   | `trg_Orders_AuditStatus`    |

  **Never use the `sp_` prefix**: SQL Server reserves it for system procedures and looks for them in `master` first.

- **Header comment** in every file, followed by exactly one `CREATE OR ALTER` statement (no `GO`, no `USE`):

```sql
-- =============================================================================
-- Object:      dbo.fn_GetOrdersByStatus
-- Type:        inline table-valued function
-- Purpose:     Returns the orders in a given status for the KDS.
-- Parameters:  @Status nvarchar(20) - order status to filter by (e.g. 'Pending')
-- Returns:     TABLE (Id uniqueidentifier, ClientId uniqueidentifier, Status nvarchar(20), CreatedAt datetime2)
-- Author:      GuillermoRH19
-- Jira:        ITACR-27
-- =============================================================================
CREATE OR ALTER FUNCTION dbo.fn_GetOrdersByStatus (@Status nvarchar(20))
RETURNS TABLE
AS
RETURN
    SELECT o.Id, o.ClientId, o.Status, o.CreatedAt
    FROM dbo.Orders AS o
    WHERE o.Status = @Status;
```

- **Re-runnable scripts**: `CREATE OR ALTER` works for procedures, functions, views, and triggers, and the migrator applies every object file on every run. It cannot change the kind of object (for example a scalar function into a table-valued one); in that case drop the old object with `DROP ... IF EXISTS` in a migration script.
- **The API only calls stored procedures** (`usp_`): its database user `itacr_app` has `EXECUTE` on `dbo` and nothing else. Name them `usp_<Entity>_<Action>` (`usp_Employee_GetById`, `usp_Order_Create`).
  - Start every procedure with `SET NOCOUNT ON;`; procedures that change data also start with `SET XACT_ABORT ON;`.
  - Return **one result set**. Dapper reads only the first one, so collect alternatives in a table variable and select it once.
  - Do read-modify-write in **one statement**, never as a read in C# followed by a write: `UPDATE ... SET Count = Count + 1 ... OUTPUT inserted.Count` cannot lose a concurrent update (see `usp_Employee_RegisterFailedPinLogin`).
  - **Inline table-valued functions** (`fn_`) and views hold query logic that several procedures reuse. Procedures read them through ownership chaining, so the API needs no `SELECT` permission. An inline function cannot have `ORDER BY`; the procedure orders the result.
- **Triggers** are the last resort; prefer service logic. When needed, they must be set-based (handle several rows in `inserted`/`deleted`).
- **Azure SQL compatibility**: no `USE`, no cross-database queries, no SQL Server Agent jobs, no CLR, and no server-level settings.
- **Deployment only through the migrator**: write or update the file in `database/<type>/<Module>/`, run `dotnet run --project database/Itacr.Database.Migrator` locally, and commit the file. There is no migration to create for an object: the file always holds its current version, and git holds the history. To delete an object, remove its file and add a migration script with `DROP ... IF EXISTS`.
- **`database/scripts/`** is for scripts run by hand: database users (`security/`), read-only reports, and local maintenance. They are never applied automatically. Anything that changes the schema or shared data must be a migration script.

### 3.3 Data access from C# (Dapper)

- Only repositories in `Infrastructure/Persistence/<Module>/` touch the database. They implement the interfaces that `Application` defines per module (`IEmployeeRepository`) and are `internal sealed`.
- Every call is a stored procedure through `DbSession.Procedure(...)`, which sets `CommandType.StoredProcedure`, the current transaction, and the `CancellationToken`. Parameters are always an object (`new { Id = id }`); never build SQL by concatenating strings.
- One `DbSession` per request (scoped): the connection opens on first use. Opening it is retried for transient errors and the serverless resume; **commands are never retried**, so nothing runs twice inside a transaction.
- Transactions are on demand: a use case that writes several rows wraps them in `IDbSession.ExecuteInTransactionAsync(...)`, which commits at the end and rolls back on any exception. Single-statement writes need no transaction.
- `DateTime` parameters are sent as `datetime2` (mapped once in `AddPersistence`).

```csharp
internal sealed class EmployeeRepository(DbSession session) : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var connection = await session.GetOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Employee>(
            session.Procedure("dbo.usp_Employee_GetById", new { Id = id }, cancellationToken));
    }
}
```

---

## 4. Language policy

| What                                          | Language |
| --------------------------------------------- | -------- |
| Code, identifiers, comments, commit messages  | English  |
| Documentation, PR descriptions, issues        | English  |
| API error `detail` messages shown to end users | Spanish  |
| Validation `ErrorMessage` values in DTOs      | Spanish  |
