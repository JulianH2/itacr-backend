# ITACR Coding Standards

These standards apply to both repositories (`itacr-frontend` and `itacr-backend`). Keep this file identical in both; change it through a Pull Request reviewed by the other team member.

Tooling enforces what it can (TypeScript strict mode, ESLint, Prettier, commitlint, CI). Everything else is checked in code review.

## Table of contents

1. [General](#1-general)
2. [Angular (itacr-frontend)](#2-angular-itacr-frontend)
3. [NestJS (itacr-backend)](#3-nestjs-itacr-backend)
4. [Database (PostgreSQL + Prisma)](#4-database-postgresql--prisma)
5. [Language policy](#5-language-policy)

---

## 1. General

### 1.1 Principles

- Follow **Clean Code** and **SOLID**. Apply **DRY** (don't repeat yourself) and **KISS** (keep it simple): the simplest solution that meets the acceptance criteria wins.
- Write small, single-purpose functions. If a function needs "and" to describe it, split it.
- Prefer **early returns** over nested `if`/`else`.
- No dead code and no commented-out code. Git keeps the history.
- No magic numbers or strings. Use named constants or enums.

```ts
// Bad
if (pin.length !== 4) { ... }

// Good
const PIN_LENGTH = 4;
if (pin.length !== PIN_LENGTH) { ... }
```

### 1.2 TypeScript

- `strict` mode is on in both projects. Do not disable it, not even per file.
- Never use `any`. Use `unknown` plus a type guard when the type is truly unknown.
- Explicit return types on public functions and methods.
- Prefer `const` over `let`, and `readonly` for properties and arrays that are not reassigned.
- Use `interface` for data contracts (DTOs, API responses, models). Use `type` for unions and utility types.
- Use `enum` or `as const` objects for closed sets of values (order status, roles).

```ts
export enum OrderStatus {
  Pending = 'PENDING',
  InKitchen = 'IN_KITCHEN',
  Ready = 'READY',
  Delivered = 'DELIVERED',
  Cancelled = 'CANCELLED',
}

function isApiError(value: unknown): value is ApiError {
  return typeof value === 'object' && value !== null && 'status' in value;
}
```

### 1.3 Naming

| Element                           | Convention                | Example                                           |
| --------------------------------- | ------------------------- | ------------------------------------------------- |
| Variables, functions, methods     | camelCase                 | `totalAmount`, `calculateTotal()`                 |
| Classes, interfaces, types, enums | PascalCase                | `OrdersService`, `CreateOrderDto`, `OrderStatus`  |
| Constants (module level)          | UPPER_SNAKE_CASE          | `MAX_ITEMS_PER_ORDER`, `ORDERS_ROUTES`            |
| File names                        | kebab-case                | `order-summary.component.ts`, `orders.service.ts` |
| Angular selectors                 | kebab-case, `app-` prefix | `app-order-summary`                               |
| Database tables and columns       | snake_case                | `orders`, `created_at`                            |
| SQL objects and their files       | snake_case with prefix    | `fn_calculate_order_total`, `vw_daily_sales`      |

- Use descriptive English names. No abbreviations except widely known ones: `id`, `url`, `dto`, `api`, `http`, `uuid`, `jwt`.
- Booleans read as questions: `isOffline`, `hasPendingOrders`, `canCancel`.
- File suffixes describe the role: `.component.ts`, `.service.ts`, `.guard.ts`, `.interceptor.ts`, `.pipe.ts`, `.directive.ts`, `.routes.ts`, `.module.ts`, `.controller.ts`, `.dto.ts`, `.spec.ts`. The Angular CLI in `itacr-frontend` is configured to generate these suffixes.

### 1.4 Formatting

- **Prettier owns formatting**: 2-space indentation, single quotes, semicolons, 100-character line width, trailing commas, LF line endings.
- The pre-commit hook formats staged files and CI runs `npm run format:check`.
- **Do not debate formatting in code reviews.** If Prettier accepts it, it is correct.

### 1.5 Comments and documentation

- Comments explain **why**, not **what**. If the code needs a comment to explain what it does, rename or refactor it first.
- Use **TSDoc** (`/** ... */`) on public APIs: exported functions, services, and DTO properties that are not self-explanatory.
- `TODO` comments must reference a Jira key: `// TODO(ITACR-15): retry with exponential backoff`.

### 1.6 Error handling

- Never swallow errors (`catch {}` with nothing inside is forbidden). Handle them, rethrow them, or log them with context.
- Never show stack traces, SQL errors, or internal messages to users. Show a friendly message in Spanish and log the details.
- Log errors with context (operation, relevant ids), never with secrets or PINs.

```ts
try {
  await this.ordersRepository.create(order);
} catch (error: unknown) {
  this.logger.error(`Failed to create order ${order.clientId}`, error);
  throw new InternalServerErrorException();
}
```

### 1.7 Security (OWASP Top 10 basics)

- **Never commit secrets.** Use `.env` (git-ignored) and keep `.env.example` updated with every variable and a safe placeholder.
- Validate and sanitize **all** input on the backend. Frontend validation is only for user experience.
- Hash PINs and passwords with **bcrypt** or **argon2**. Never store or log them in plain text.
- Apply least privilege: every endpoint and route checks the role (waiter, kitchen, manager, customer).
- Keep dependencies updated: review Dependabot PRs every week and run `npm audit` before each sprint close.

### 1.8 Testing

- Unit tests for services and business rules (totals, order status transitions, sync logic, validation).
- Test names follow `should <expected behavior> when <condition>`.
- Structure every test as **Arrange, Act, Assert**.
- Write at least one test per acceptance criterion when feasible.
- Tests must be deterministic: no real network, no real dates (mock the clock), no order dependency between tests.

```ts
it('should reject the PIN when it has fewer than 4 digits', () => {
  // Arrange
  const pin = '123';

  // Act
  const result = validatePin(pin);

  // Assert
  expect(result.isValid).toBe(false);
});
```

---

## 2. Angular (itacr-frontend)

### 2.1 Structure

Follow the official [Angular Style Guide](https://angular.dev/style-guide).

```
src/app/
├── core/          # Singletons used app-wide: guards, interceptors, services, models
├── shared/        # Reusable presentational components, pipes, directives (no services with state)
└── features/      # One folder per epic, each lazy-loaded
    ├── auth/
    ├── catalog/
    ├── orders/
    ├── kitchen/
    ├── tracking/
    └── manager/
        ├── manager.routes.ts
        ├── pages/          # Routed (smart) components
        ├── components/     # Presentational components used only by this feature
        ├── services/
        └── models/
```

- A feature never imports from another feature. Shared code moves to `shared/` or `core/`.
- Each feature exposes a `<feature>.routes.ts` file lazy-loaded from `app.routes.ts` with `loadChildren`.

### 2.2 Components

- Standalone components only (no `NgModule`).
- `changeDetection: ChangeDetectionStrategy.OnPush` on every component (the CLI is configured to add it).
- **Signals** for local state (`signal`, `computed`, `input`, `output`, `model`).
- New control flow syntax: `@if`, `@for` (always with `track`), `@switch`, `@defer`. Do not use `*ngIf` or `*ngFor`.
- **Smart vs. presentational**:
  - Pages (smart) inject services, read route data, and pass data down.
  - Presentational components only use `input()` and `output()`, with no injected services.
- Components contain **no business logic and no HTTP calls**. That belongs in services.

### 2.3 Forms, HTTP, and routing

- Typed reactive forms (`FormGroup<{ pin: FormControl<string> }>` or `NonNullableFormBuilder`).
- Functional HTTP interceptors (`withInterceptors`) for the auth token and for error handling.
- Functional route guards for role-based access (`waiter`, `kitchen`, `manager`).
- Services return typed `Observable<T>` or signals. Never return `any`.

### 2.4 Subscriptions

- Prefer the `async` pipe or `toSignal()`.
- When a manual `subscribe()` is unavoidable, use `takeUntilDestroyed()`.

### 2.5 PWA and offline

- Version the service worker caches in `ngsw-config.json`: increase `appData.cacheVersion` and the `version` of a data group when its response format changes.
- The catalog (`/api/v1/products`, `/categories`, `/combos`) uses the `freshness` strategy. Product images (`/uploads/**`) use `performance`.
- Orders are **never** cached by the service worker. They go to IndexedDB and are synced with Background Sync.
- IndexedDB naming: database `itacr`, stores in kebab-case plural (`pending-orders`, `catalog-snapshot`). Keep store names in constants in one file.
- Every order gets a client-generated UUID (`crypto.randomUUID()`) that the API uses as an idempotency key, so a retried sync never creates duplicates.
- Never block the UI while offline. Show the connection state and queue the action.

### 2.6 Accessibility

- Semantic HTML (`header`, `nav`, `main`, `button` for actions, `a` for navigation).
- Every input has a visible `<label>` or `mat-label`.
- Sufficient color contrast (WCAG AA). Use the Material `--mat-sys-*` variables, not hard-coded colors.
- Touch targets of at least **44x44 px**.

---

## 3. NestJS (itacr-backend)

### 3.1 Modules

- One module per feature: `auth`, `catalog`, `orders`, `kitchen`, `notifications`, `manager`, plus `health` and a `prisma` module.
- **Controllers stay thin**: they validate input through DTOs, call one service method, and return the result.
- Business logic lives in services. Data access goes through Prisma, in services or repository classes.

### 3.2 Validation and configuration

- A DTO with `class-validator` decorators for every input (body, query, params).
- Global `ValidationPipe` with `{ whitelist: true, forbidNonWhitelisted: true, transform: true }`.
- A global exception filter that returns the standard error format (see 3.3).
- Configuration through `@nestjs/config` with schema validation at startup. The app must fail fast if a variable is missing.

### 3.3 REST conventions

- Global prefix `/api/v1`.
- Plural nouns: `/orders`, `/products`, `/orders/:id/items`. Actions that are not CRUD use a sub-resource: `PATCH /orders/:id/status`.
- Correct methods and status codes: `200` OK, `201` Created, `204` No Content, `400` validation, `401` unauthenticated, `403` forbidden, `404` not found, `409` conflict, `422` business rule violation, `429` rate limited.
- Errors use **RFC 9457 Problem Details** (`application/problem+json`):

```json
{
  "type": "https://itacr.app/problems/validation-error",
  "title": "Validation failed",
  "status": 400,
  "detail": "El PIN debe tener exactamente 4 dígitos.",
  "instance": "/api/v1/auth/pin"
}
```

- Paginate every list: `?page=1&limit=20`, and return `{ data, meta: { page, limit, total } }`.
- Dates in **ISO 8601 UTC** (`2026-09-28T18:30:00.000Z`). The frontend converts to local time.
- Order creation accepts the client UUID as an **idempotency key**. Repeating the request with the same key returns the original order instead of creating a duplicate.

### 3.4 Security and operations

- `helmet`, strict CORS (only the frontend origins from `.env`), and rate limiting with `@nestjs/throttler`.
- JWT authentication with role guards (`@Roles('manager')`).
- Structured logging (JSON in production) with a request id.
- `GET /api/v1/health` for liveness checks.
- Document every endpoint with Swagger decorators (`@ApiTags`, `@ApiOperation`, `@ApiResponse`, DTO `@ApiProperty`).

---

## 4. Database (PostgreSQL + Prisma)

The database lives in `itacr-backend`:

```
prisma/
├── schema.prisma     # Source of truth for tables, enums, relations, and indexes
├── migrations/       # Generated by Prisma; applied in order in every environment
└── seed.ts           # Idempotent seed data
database/             # SQL objects that Prisma does not model, one object per file
├── procedures/       # sp_*.sql
├── functions/        # fn_*.sql
├── views/            # vw_*.sql
├── triggers/         # trg_*.sql
└── scripts/          # One-off maintenance or reporting scripts (never applied automatically)
```

**No manual changes in the database.** Every change to tables or SQL objects reaches the database through a Prisma migration, so it is versioned and applied in the same order on every machine and environment.

### 4.1 Tables with Prisma

- Prisma models in **PascalCase singular**, mapped to **snake_case plural** tables with `@@map`. Columns are mapped to snake_case with `@map`.

```prisma
model Order {
  id        String      @id @default(uuid()) @db.Uuid
  clientId  String      @unique @map("client_id") @db.Uuid
  status    OrderStatus @default(PENDING)
  waiterId  String?     @map("waiter_id") @db.Uuid
  createdAt DateTime    @default(now()) @map("created_at")
  updatedAt DateTime    @updatedAt @map("updated_at")

  @@index([status])
  @@index([createdAt])
  @@index([waiterId])
  @@map("orders")
}
```

- Every schema change goes through a migration (`npx prisma migrate dev --name <change>`). **Never edit a migration that was already applied** on another machine or merged into `sprint-XX`, `develop`, `release`, or `main`. Create a new one.
- Seeds are **idempotent** (`upsert`), so running them twice does not duplicate data.
- Every table has `created_at` and `updated_at`.
- Use foreign keys, and add indexes on frequently filtered columns (`status`, `created_at`, `waiter_id`).
- Use `JSONB` (`Json` in Prisma) for combo customizations.
- Money is stored as `Decimal` (`@db.Decimal(10, 2)`), never as a float.

### 4.2 SQL objects: procedures, functions, views, and triggers

- **One object per file** in `database/`, named exactly like the object: `database/functions/fn_get_orders_by_status.sql`.
- **Naming**: snake_case with a prefix by type. Parameters start with `p_` and local variables with `v_`.

  | Object           | Prefix | Folder                 | Example                     |
  | ---------------- | ------ | ---------------------- | --------------------------- |
  | Stored procedure | `sp_`  | `database/procedures/` | `sp_close_cash_register`    |
  | Function         | `fn_`  | `database/functions/`  | `fn_get_orders_by_status`   |
  | View             | `vw_`  | `database/views/`      | `vw_daily_sales`            |
  | Trigger          | `trg_` | `database/triggers/`   | `trg_orders_set_updated_at` |

- **Header comment** in every file:

```sql
-- =============================================================================
-- Object:      fn_get_orders_by_status
-- Type:        function
-- Purpose:     Returns the orders in a given status, oldest first, for the KDS.
-- Parameters:  p_status text - order status to filter by (e.g. 'PENDING')
-- Returns:     TABLE (id uuid, client_id uuid, status text, created_at timestamptz)
-- Author:      GuillermoRH19
-- Jira:        ITACR-27
-- =============================================================================
CREATE OR REPLACE FUNCTION fn_get_orders_by_status(p_status text)
RETURNS TABLE (id uuid, client_id uuid, status text, created_at timestamptz)
LANGUAGE sql
STABLE
AS $$
  SELECT o.id, o.client_id, o.status::text, o.created_at
  FROM orders o
  WHERE o.status::text = p_status
  ORDER BY o.created_at;
$$;
```

- **Re-runnable scripts**: use `CREATE OR REPLACE` (functions, procedures, views, and triggers on PostgreSQL 14+). PostgreSQL does not allow `CREATE OR REPLACE` to change a function's return type or to remove or rename a view's columns; in those cases add `DROP ... IF EXISTS` before the `CREATE` in the same migration.
- **Procedures vs. functions in PostgreSQL**: a procedure (`CALL sp_...`) cannot return rows. Queries that return data (such as "get orders by status") are functions (`fn_`) that return `TABLE (...)`. Use procedures (`sp_`) for operations that change data or need transaction control.
- **Deployment only through Prisma migrations**:
  1. Write or update the file in `database/<type>/`.
  2. Create an empty migration: `npx prisma migrate dev --create-only --name add_fn_get_orders_by_status`.
  3. Copy the full content of the SQL file into the new `prisma/migrations/<timestamp>_add_fn_get_orders_by_status/migration.sql`.
  4. Apply it: `npx prisma migrate dev`. Commit the `database/` file and the migration together.
  5. To change an object later, edit its file in `database/` and create a **new** migration with the new full definition. The file in `database/` always holds the current version; the migrations hold the history.
- **Calling SQL objects from NestJS**: only with parameterized Prisma queries (`this.prisma.$queryRaw` with a tagged template). Never build SQL by concatenating strings, and never use `$queryRawUnsafe` with user input.
- **`database/scripts/`** is for read-only reports and local maintenance. Scripts are never applied automatically. Anything that changes the schema or shared data must be a migration.

---

## 5. Language policy

| What                                            | Language |
| ----------------------------------------------- | -------- |
| Code, identifiers, comments, commit messages    | English  |
| Documentation, PR descriptions, issues          | English  |
| User-facing UI text (labels, buttons, messages) | Spanish  |
| API error `detail` messages shown to end users  | Spanish  |
