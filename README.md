# itacr-backend

**ITACR** is a Progressive Web App (PWA) for managing fast-food restaurants in Mexico. Waiters take orders from their phone or tablet (even offline), customers place pickup orders, the kitchen works from a Kitchen Display System (KDS), and managers review metrics.

This repository contains the backend: the ASP.NET Core REST API, real-time kitchen updates, push notifications, and the database (versioned SQL scripts, T-SQL procedures, functions, views, and triggers, and the DbUp migrator that applies them). The Angular PWA lives in [itacr-frontend](https://github.com/JulianH2/itacr-frontend).

> Course project: Progressive Web Applications, Universidad Tecnológica de Tula-Tepeji (UTTT), group 10IDGS-G3.

## Tech stack

| Area           | Technology                                                                              |
| -------------- | --------------------------------------------------------------------------------------- |
| Runtime        | .NET 10 (LTS), C# with nullable reference types                                         |
| API            | ASP.NET Core Web API (controllers), Problem Details (RFC 9457), built-in rate limiting  |
| API docs       | OpenAPI (`Microsoft.AspNetCore.OpenApi`) + Scalar UI                                     |
| Database       | Azure SQL Database (production), SQL Server 2025 in Docker (local development)          |
| Data access    | Dapper over stored procedures (`Microsoft.Data.SqlClient`), one repository per module   |
| Schema         | SQL scripts applied by `Itacr.Database.Migrator` (DbUp)                                  |
| Real time      | ASP.NET Core SignalR (KDS and order tracking)                                           |
| Notifications  | Web Push with VAPID keys                                                                |
| Auth           | JWT bearer tokens with role-based authorization                                         |
| Tests          | xUnit, `WebApplicationFactory` for API tests                                            |
| Quality        | `.editorconfig` + analyzers (warnings are errors in CI), `dotnet format`, git hooks     |
| CI and release | GitHub Actions, Dependabot, release-please                                              |
| Hosting        | Azure App Service (Linux, .NET) + Azure SQL Database                                    |

## Prerequisites

- **.NET SDK 10** (see `global.json`): `dotnet --version` must print `10.0.x`
- **Docker Desktop** (SQL Server runs in a container)
- Git
- Recommended VS Code extensions in `.vscode/extensions.json` (C# Dev Kit, EditorConfig, Docker, SQL Server, REST Client), or Visual Studio 2026 / Rider

## Getting started

```bash
git clone https://github.com/JulianH2/itacr-backend.git
cd itacr-backend
git config core.hooksPath .githooks
cp .env.example .env                 # PowerShell: Copy-Item .env.example .env
docker compose up -d sqlserver
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=Itacr;User Id=sa;Password=<MSSQL_SA_PASSWORD from .env>;TrustServerCertificate=True" --project src/Itacr.Api
dotnet user-secrets set "Jwt:Secret" "<random value, at least 64 characters>" --project src/Itacr.Api
dotnet user-secrets set "ConnectionStrings:Migrator" "Server=localhost,1433;Database=Itacr;User Id=sa;Password=<MSSQL_SA_PASSWORD from .env>;TrustServerCertificate=True" --project database/Itacr.Database.Migrator
dotnet run --project database/Itacr.Database.Migrator
dotnet run --project src/Itacr.Api
```

Edit `.env` before starting the database and set a strong `MSSQL_SA_PASSWORD`. The API does not start without `Jwt:Secret` (at least 64 characters); [CONTRIBUTING.md, section 1](CONTRIBUTING.md#1-one-time-setup) shows how to generate it and has the full one-time setup.

The API listens on `http://localhost:3000`:

| URL                                     | What                                       |
| --------------------------------------- | ------------------------------------------ |
| `http://localhost:3000/api/v1`          | REST API                                   |
| `http://localhost:3000/api/v1/health`   | Health check (API and database)            |
| `http://localhost:3000/scalar/v1`       | API reference UI (Development only)        |
| `http://localhost:3000/openapi/v1.json` | OpenAPI document (Development only)        |
| `http://localhost:3000/hubs/kitchen`    | SignalR hub for the Kitchen Display System |

## Commands

| Command                                                 | Description                                                               |
| ------------------------------------------------------- | ------------------------------------------------------------------------- |
| `dotnet run --project src/Itacr.Api`                    | Run the API at `http://localhost:3000`                                    |
| `dotnet watch --project src/Itacr.Api`                  | Run with hot reload                                                       |
| `dotnet build`                                          | Build the solution (Debug)                                                |
| `dotnet build -c Release`                               | Build like CI: warnings are errors                                        |
| `dotnet test`                                           | Run all tests (Docker must be running: the migrator tests use SQL Server) |
| `dotnet format`                                         | Format and fix code style with `.editorconfig`                            |
| `dotnet format --verify-no-changes`                     | Check formatting (used by CI)                                             |
| `dotnet run --project database/Itacr.Database.Migrator` | Create or update the local database: scripts, catalog, and test employees |
| `dotnet list package --vulnerable --include-transitive` | List NuGet packages with known vulnerabilities                            |
| `docker compose up -d sqlserver`                        | Start SQL Server                                                          |
| `docker compose down`                                   | Stop SQL Server (data is kept)                                            |
| `docker compose down -v`                                | Stop SQL Server and delete all local data                                 |

## Local development

`dotnet run --project database/Itacr.Database.Migrator` creates the database, applies every script in `database/` (schema and catalog: categories, products, combo options, and tables 1 to 10), and, because `dotnet run` uses the `Development` environment, creates four test employees, all with the password `Itacr2026!`. Run it again after every pull that brings new scripts; it only applies what is missing.

| Role    | Email                  | PIN    | Status           |
| ------- | ---------------------- | ------ | ---------------- |
| Waiter  | `mesero@itacr.test`    | `1111` | Active           |
| Kitchen | `cocina@itacr.test`    | `2222` | Active           |
| Manager | `gerente@itacr.test`   | `3333` | Active           |
| Waiter  | `pendiente@itacr.test` | `4444` | Pending approval |

These PINs and passwords are for local development only and are never created in any other environment. See [database/README.md](database/README.md#seed-data) for what is seeded and why.

## Folder structure

```
itacr-backend/
├── .github/                    # CI and release-please workflows, Dependabot, PR and issue templates, CODEOWNERS
├── .githooks/                  # Git hooks: pre-commit (dotnet format), commit-msg (commit convention)
├── database/                   # migrations/ (V001__...), functions/, views/, procedures/, triggers/ per module, scripts/, Itacr.Database.Migrator/
├── docs/
│   ├── CODING_STANDARDS.md
│   └── database.md
├── src/                        # Clean Architecture: references point inward only (see docs/CODING_STANDARDS.md, section 2.1)
│   ├── Itacr.Domain/           # Entities and enums per module: Identity/, Catalog/, Orders/, Customers/, Notifications/
│   ├── Itacr.Application/      # Services, interfaces, requests, and responses per module: Identity/, Catalog/
│   ├── Itacr.Infrastructure/
│   │   ├── Persistence/        # DbSession, <Module>/ repositories with Dapper, database health check
│   │   └── Identity/           # JWT token service
│   └── Itacr.Api/
│       ├── Endpoints/          # One folder per module with its controllers: Identity/, Catalog/
│       ├── Common/             # Pipeline, options, rate limiting, security headers
│       ├── Program.cs
│       └── appsettings.json
├── tests/
│   └── Itacr.Api.Tests/
├── compose.yaml                # Local SQL Server
├── Directory.Build.props       # Shared build settings and the version (updated by release-please)
├── Directory.Packages.props    # Central NuGet package versions
├── global.json                 # .NET SDK version
└── Itacr.slnx
```

The API, the tests, the solution, and `compose.yaml` are created in ITACR-26, and `database/` in ITACR-27 ([CONTRIBUTING.md, section 14](CONTRIBUTING.md#14-bootstrapping-the-backend-itacr-26-itacr-27-itacr-28)).

## Configuration

| Where                                        | What                                                                    | Committed |
| -------------------------------------------- | ----------------------------------------------------------------------- | --------- |
| `.env`                                       | Local SQL Server `sa` password (read by Docker Compose)                  | No        |
| `src/Itacr.Api/appsettings.json`             | Non-secret settings: CORS origins, JWT issuer and lifetime, rate limits | Yes       |
| `src/Itacr.Api/appsettings.Development.json` | Development overrides without secrets (log levels)                      | Yes       |
| `dotnet user-secrets`                        | Local secrets: connection string and JWT secret                         | No        |
| Azure App Service environment variables      | Production values (see [Deployment](#deployment))                       | No        |

Every settings section is bound to an options class validated at startup, so the API fails fast when a value is missing.

## Deployment

Production runs on Azure (resource group `rg-itacr-prod-westus`, region West US):

| Resource           | Name                     | Notes                                                        |
| ------------------ | ------------------------ | ------------------------------------------------------------ |
| Azure SQL server   | `itacr-sql`              | Free offer (serverless, pauses when idle)                    |
| Azure SQL database | `itacr-sql`              |                                                              |
| App Service plan   | `asp-itacr-prod-westus`  | Linux, F1 Free, shared with the frontend                     |
| App Service        | `app-itacr-backend-prod` | .NET 10, HTTPS only, TLS 1.2, FTP disabled, WebSockets on    |

| URL          | Value                                                                           |
| ------------ | ------------------------------------------------------------------------------- |
| API          | `https://app-itacr-backend-prod-b3hvcggncghafbgp.westus-01.azurewebsites.net/api/v1` |
| Health check | `https://app-itacr-backend-prod-b3hvcggncghafbgp.westus-01.azurewebsites.net/api/v1/health` |
| Frontend     | `https://app-itacr-frontend-prod-hchjfvaadffrhre3.westus-01.azurewebsites.net`  |

Azure generates the domains with a unique suffix (`<app>-<suffix>.westus-01.azurewebsites.net`); `app-itacr-backend-prod.azurewebsites.net` does not exist. They are shown in each App Service > Overview > **Default domain**.

The health check returns `200 Healthy` when the API runs and reaches Azure SQL, and `503 Unhealthy` when it cannot. The free serverless database pauses when idle, so the first request after a pause can take up to a minute or return `503` once while it resumes.

### Production settings

They live only in App Service `app-itacr-backend-prod` > Settings > **Environment variables**, never in the repository. The API validates them at startup and does not start if one is missing (the log stream says which one).

| Variable                     | Value                                                                                       |
| ---------------------------- | ------------------------------------------------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT`     | `Production`                                                                                |
| `ConnectionStrings__Default` | `Server=tcp:itacr-sql.database.windows.net,1433;Initial Catalog=itacr-sql;User ID=itacr_app;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60` |
| `Jwt__Secret`                | Random value of at least 64 characters, different from every development secret            |
| `Cors__Origins__0`           | `https://app-itacr-frontend-prod-hchjfvaadffrhre3.westus-01.azurewebsites.net`              |

- `Cors:Origins` is a list, so each origin is its own variable (`Cors__Origins__0`, `Cors__Origins__1`, ...). A single `Cors__Origins` variable is ignored, the list stays empty, and the API does not start.
- `Connection Timeout=60` gives the serverless database time to resume after a pause.

## Branching model

```
personal branches                    sprint            integration     QA/staging      production

feature/ITACR-7-pin-login --+
fix/ITACR-9-order-total ----+--PR--> sprint-01 --PR--> develop --PR--> release --PR--> main --> vX.Y.Z tag
docs/ITACR-29-readme -------+                                             ^             |      (release-please)
                                                                          |             |
                                                                          +-back-merge--+

hotfix/ITACR-N-description (from main) --PR--> main, then the same branch --PR--> develop
```

- `main` (production), `release` (QA/staging), and `develop` (integration, default branch) are protected: PR required, 1 approval, green CI, no direct or force pushes.
- `sprint-XX` is created from `develop` every Monday and only receives PRs from personal branches.
- Personal branches: `<type>/ITACR-<N>-<short-description>`, one per Jira issue, created from `sprint-XX` and deleted after merge. `type` is `feature`, `fix`, `docs`, `refactor`, `test`, or `chore`.
- Every Friday: `sprint-XX` → `develop` → `release` (QA) → `main`. release-please then creates the version tag (`v0.1.0`, `v0.2.0`, ...), `CHANGELOG.md`, and the GitHub Release on `main`, and `main` is back-merged into `release`.
- Hotfixes: `hotfix/ITACR-<N>-<description>` from `main`, PR into `main`, then the same branch into `develop`.
- CI validates that each PR comes from an allowed branch and prints the allowed flow when it does not.

Full workflow and sprint close checklist: [CONTRIBUTING.md](CONTRIBUTING.md).

## Commit convention

```
type(scope): short description (ITACR-N)
feat(orders): add idempotent order creation (ITACR-9)
```

Checked by the `commit-msg` git hook and again by CI. Types, scopes, and details are in [CONTRIBUTING.md](CONTRIBUTING.md#3-commit-convention).

## Team

| Member                          | GitHub                                             |
| ------------------------------- | -------------------------------------------------- |
| José Julián Hernández Hernández | [@JulianH2](https://github.com/JulianH2)           |
| José Guillermo Rangel Hernández | [@GuillermoRH19](https://github.com/GuillermoRH19) |

Instructor: Juan Carlos Benítez Reyes. The Scrum Master role rotates every sprint.

## Project management

- Jira project **ITACR**: `https://<your-site>.atlassian.net/jira/software/projects/ITACR/boards` (placeholder: replace with the real board URL)
- Sprints: weekly, Monday to Friday. Sprint 1: 2026-09-28 to 2026-10-02.

## Related documents

- [CONTRIBUTING.md](CONTRIBUTING.md): daily workflow, sprints, reviews, database workflow, Definition of Done
- [docs/CODING_STANDARDS.md](docs/CODING_STANDARDS.md)
- [database/README.md](database/README.md)
- [CHANGELOG.md](CHANGELOG.md) (generated by release-please)
- [SECURITY.md](SECURITY.md)
