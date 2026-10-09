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
| Private fields                | `_camelCase`                                  | `_dbContext`, `_timeProvider`                   |
| Constants, static readonly    | PascalCase                                    | `MaxItemsPerOrder`                              |
| Files                         | PascalCase, one type per file, named like the type | `OrdersService.cs`                         |
| Tables and columns            | PascalCase (EF Core default), tables plural   | `dbo.Orders`, `CreatedAt`                       |
| T-SQL objects and their files | prefix + PascalCase                           | `usp_CloseCashRegister`, `fn_GetOrdersByStatus` |

- Use descriptive English names. No abbreviations except widely known ones: `Id`, `Url`, `Dto`, `Api`, `Http`, `Uuid`, `Jwt`.
- Booleans read as questions: `IsOffline`, `HasPendingOrders`, `CanCancel`.
- Type names describe the role: `OrdersController`, `OrdersService`, `CreateOrderRequest`, `OrderResponse`, `OrderConfiguration` (EF Core), `KitchenHub` (SignalR), `JwtOptions` (settings), `OrdersServiceTests`.

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
    await _dbContext.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: UniqueKeyViolation or UniqueIndexViolation })
{
    // The same client UUID was synced twice: return the order that already exists.
    _logger.LogInformation("Order {ClientId} already exists; returning the existing one", request.ClientId);
    return await GetByClientIdAsync(request.ClientId, cancellationToken);
}
```

`UniqueKeyViolation` (`2627`) and `UniqueIndexViolation` (`2601`) are named constants for the SQL Server error numbers.

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
│   └── Common/Persistence/   # IItacrDbContext, implemented by Infrastructure
├── Itacr.Infrastructure/     # Persistence/ (ItacrDbContext, Configurations/, Migrations/, Seeding/), Identity/ (JWT)
└── Itacr.Api/                # Endpoints/<Module>/ (controllers), Common/ (pipeline, options, security headers), Program.cs
```

| Project | References | Contains |
| ------- | ---------- | -------- |
| `Itacr.Domain` | nothing | Entities, enums, and rules that need no I/O |
| `Itacr.Application` | `Domain` | Business logic and the interfaces it needs (`ITokenService`, `IItacrDbContext`) |
| `Itacr.Infrastructure` | `Application`, `Domain` | Implementations of those interfaces: database, JWT |
| `Itacr.Api` | `Application`, `Infrastructure` | HTTP only: routing, validation, status codes, and DI composition |

- A module has the same name in every project (`Domain/Catalog`, `Application/Catalog`, `Api/Endpoints/Catalog`), so a change to one module touches folders with the same name.
- `Domain` never uses EF Core, ASP.NET Core, or any other package. `Application` never references `Infrastructure` or `Api`.
- A module never calls another module's controller. Shared logic moves to `Domain/` or is exposed through a service interface.
- **Controllers stay thin**: they receive a validated request, call one service method, and map the result to an HTTP response.
- Business logic lives in `Application` services, registered in DI behind an interface (`ICatalogService`). Until ITACR-36 replaces EF Core, data access goes through `IItacrDbContext`. Do not add a generic repository.
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
- Every endpoint accepts a `CancellationToken` and passes it down to EF Core.

### 2.4 Security and operations

- Security headers on every response (`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`), HSTS and HTTPS redirection outside Development.
- Strict CORS: only the frontend origins from `Cors:Origins` (the local dev server, and in production the frontend App Service, set with the `Cors__Origins__0` environment variable). Never `AllowAnyOrigin` together with credentials.
- Rate limiting with the built-in `AddRateLimiter` (a global limiter plus a stricter policy on the PIN login endpoint).
- JWT bearer authentication with role-based authorization: `[Authorize(Roles = nameof(EmployeeRole.Manager))]`. The token lasts one shift (`Jwt:TokenLifetimeHours`, 12) and carries the `sub` (employee id), `name`, and `role` claims; there is no refresh token. Controllers are closed by default through a global `AuthorizeFilter`; public endpoints use `[AllowAnonymous]` explicitly, and SignalR hubs need `[Authorize]` on the hub class.
- PIN login is locked per employee after `LoginSecurity:MaxFailedPinAttempts` (5) wrong PINs for `LoginSecurity:LockoutMinutes` (15), and the login endpoints have their own stricter rate limit (`LoginSecurity:PermitLimit` per minute and IP) on top of the global one.
- Structured logging with message templates (`_logger.LogInformation("Order {OrderId} created", order.Id)`), never string interpolation in log calls. JSON console logs outside Development. Every log and Problem Details response includes the trace id.
- `GET /api/v1/health` checks the API and the database (`AddHealthChecks().AddDbContextCheck<ItacrDbContext>()`). It is anonymous and returns no internal details.
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
├── ItacrDbContext.cs        # EF Core mapping used by the API until ITACR-36 replaces it with Dapper
└── Configurations/          # Must keep matching the tables created by the scripts
```

**No manual changes in the database.** Every change to tables or T-SQL objects is a script in `database/` applied by the migrator, so it is versioned and applied in the same order on every machine and environment.

**Until ITACR-36 is merged, the schema is frozen:** the API still maps the tables with EF Core, and CI still checks that the EF Core model has no pending changes. ITACR-36 removes EF Core; after that, a schema change is only a new script in `migrations/`.

### 3.1 Tables

- Register the context with `UseAzureSql(connectionString, sql => sql.EnableRetryOnFailure())` in every environment. The retry policy matters on Azure SQL: the free serverless database pauses when idle and the first connection after a pause can fail or be slow. With retries on, wrap explicit transactions in `_dbContext.Database.CreateExecutionStrategy().ExecuteAsync(...)`.
- Entities in **PascalCase singular** (`Order`), `DbSet` properties in **PascalCase plural** (`Orders`). EF Core maps them to tables in the `dbo` schema named like the `DbSet` (`dbo.Orders`) with PascalCase columns, so no `ToTable` or `HasColumnName` is needed.
- Configure every entity in its own `IEntityTypeConfiguration<T>` class in `Configurations/`, never with attributes on the entity.

```csharp
public sealed class Order
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public OrderStatus Status { get; set; }
    public Guid? WaiterId { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    private const int StatusMaxLength = 20;

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasIndex(order => order.ClientId).IsUnique();
        builder.HasIndex(order => order.Status);
        builder.HasIndex(order => order.CreatedAt);
        builder.HasIndex(order => order.WaiterId);
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(StatusMaxLength);
        builder.Property(order => order.Total).HasPrecision(10, 2);
    }
}
```

- Every schema change is a new script in `database/migrations/`, named `V<NNN>__<snake_case_description>.sql` (`V002__add_order_status_history.sql`). **Never edit or rename a script that was already merged** into `sprint-XX`, `develop`, `release`, or `main`: the journal (`dbo.SchemaVersions`) stores its name, so it would not run again. Create a new one.
- Keep schema changes additive (new tables, nullable or defaulted columns): the previous API runs for a moment against the new schema during a deploy. Rename or drop columns in a later release.
- Seeds are **idempotent**: reference data with fixed IDs goes in a migration; employees are seeded by the migrator, which checks existing emails first, so running it twice does not duplicate data.
- Every table has `CreatedAt` and `UpdatedAt` (`datetime2`, UTC), set by a `SaveChanges` interceptor that uses `TimeProvider`.
- Use foreign keys, and add indexes on frequently filtered columns (`Status`, `CreatedAt`, `WaiterId`). Set a maximum length on every string (`HasMaxLength`); never leave `nvarchar(max)` by accident.
- Enums are stored as text (`HasConversion<string>()`), so T-SQL objects can compare them as strings.
- Combo customizations are a JSON column (`ToJson()` on the complex or owned property), stored in the native `json` type of Azure SQL and SQL Server 2025.
- Money is `decimal` with `HasPrecision(10, 2)` (`decimal(10,2)`), never `float` or `double`.
- Primary keys are `Guid` (`uniqueidentifier`) generated by EF Core, which uses sequential values for SQL Server to avoid index fragmentation. Do not assign `Guid.NewGuid()` to keys yourself.

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
- **Functions vs. procedures**:
  - Queries that return data are **inline table-valued functions** (`fn_`): EF Core can compose them (`WHERE`, `ORDER BY`, paging) and they are parameterized. An inline function cannot have `ORDER BY`; the caller orders the result.
  - Operations that change data or need a transaction are **procedures** (`usp_`). Start every procedure with `SET NOCOUNT ON;` and `SET XACT_ABORT ON;`, and wrap multi-statement changes in `BEGIN TRANSACTION` / `COMMIT TRANSACTION`.
- **Triggers** are the last resort; prefer EF Core interceptors or service logic. When needed, they must be set-based (handle several rows in `inserted`/`deleted`).
- **Azure SQL compatibility**: no `USE`, no cross-database queries, no SQL Server Agent jobs, no CLR, and no server-level settings.
- **Deployment only through the migrator**: write or update the file in `database/<type>/<Module>/`, run `dotnet run --project database/Itacr.Database.Migrator` locally, and commit the file. There is no migration to create for an object: the file always holds its current version, and git holds the history. To delete an object, remove its file and add a migration script with `DROP ... IF EXISTS`.
- **Calling T-SQL objects from C#**: only with parameterized EF Core queries built from an interpolated string: `FromSql($"...")`, `SqlQuery<T>($"...")`, or `ExecuteSqlAsync($"...")`. EF Core turns each interpolated value into a parameter. Never build SQL by concatenating strings, and never pass user input to `FromSqlRaw`, `SqlQueryRaw`, or `ExecuteSqlRawAsync`.

```csharp
var orders = await _dbContext.Database
    .SqlQuery<KitchenOrderRow>($"SELECT * FROM dbo.fn_GetOrdersByStatus({status})")
    .OrderBy(row => row.CreatedAt)
    .ToListAsync(cancellationToken);

await _dbContext.Database.ExecuteSqlAsync(
    $"EXEC dbo.usp_CloseCashRegister @CashRegisterId = {cashRegisterId}, @ClosedBy = {userId}",
    cancellationToken);
```

- **`database/scripts/`** is for scripts run by hand: database users (`security/`), read-only reports, and local maintenance. They are never applied automatically. Anything that changes the schema or shared data must be a migration script.

---

## 4. Language policy

| What                                          | Language |
| --------------------------------------------- | -------- |
| Code, identifiers, comments, commit messages  | English  |
| Documentation, PR descriptions, issues        | English  |
| API error `detail` messages shown to end users | Spanish  |
| Validation `ErrorMessage` values in DTOs      | Spanish  |
