# itacr-backend

**ITACR** is a Progressive Web App (PWA) for managing fast-food restaurants in Mexico. Waiters take orders from their phone or tablet (even offline), customers place pickup orders, the kitchen works from a Kitchen Display System (KDS), and managers review metrics.

This repository contains the backend: the NestJS REST API, real-time kitchen updates, push notifications, and the PostgreSQL database (Prisma schema, migrations, seeds, and SQL procedures, functions, views, and triggers). The Angular PWA lives in [itacr-frontend](https://github.com/JulianH2/itacr-frontend).

> Course project: Progressive Web Applications, Universidad Tecnológica de Tula-Tepeji (UTTT), group 10IDGS-G3.

> **Status:** repository-level setup only. The NestJS app (ITACR-26), the Prisma schema (ITACR-27), and the seed data (ITACR-28) are created by following [CONTRIBUTING.md, section 14](CONTRIBUTING.md#14-bootstrapping-the-backend-itacr-26-itacr-27-itacr-28).

## Tech stack

| Area           | Technology                                                                      |
| -------------- | ------------------------------------------------------------------------------- |
| Framework      | NestJS (latest stable), TypeScript strict mode                                  |
| Database       | PostgreSQL 18 (Docker Compose for local development)                            |
| ORM and schema | Prisma (schema, migrations, seeds) + versioned SQL objects in `database/`       |
| Real time      | `@nestjs/websockets` with Socket.IO (KDS updates)                               |
| Notifications  | `web-push` with VAPID keys                                                      |
| Validation     | `class-validator`, `class-transformer`, `@nestjs/config` with schema validation |
| Docs           | Swagger (OpenAPI) via `@nestjs/swagger`                                         |
| Quality        | ESLint (typescript-eslint), Prettier, Husky, lint-staged, commitlint            |
| CI and release | GitHub Actions, Dependabot, release-please                                      |

## Prerequisites

- **Node.js 24 LTS** (see `.nvmrc`) and npm 11
- Git
- **Docker Desktop** (for the local PostgreSQL database)

## Getting started

```bash
git clone https://github.com/JulianH2/itacr-backend.git
cd itacr-backend
npm ci
cp .env.example .env        # PowerShell: Copy-Item .env.example .env
# edit .env and replace every "change-me" value
npm run db:up               # starts PostgreSQL on localhost:5432
```

Once the NestJS app and the Prisma schema exist:

```bash
npx prisma migrate dev      # applies all migrations to your local database
npx prisma db seed          # loads the seed data (safe to run more than once)
npm run start:dev           # http://localhost:3000/api/v1, Swagger at http://localhost:3000/api/docs
```

## npm scripts

Available now:

| Script                 | Description                                            |
| ---------------------- | ------------------------------------------------------ |
| `npm run db:up`        | Start PostgreSQL in Docker                             |
| `npm run db:tools`     | Start PostgreSQL and pgAdmin (`http://localhost:5050`) |
| `npm run db:logs`      | Follow the PostgreSQL logs                             |
| `npm run db:down`      | Stop the containers (data is kept)                     |
| `npm run format`       | Format every file with Prettier                        |
| `npm run format:check` | Check formatting (used by CI)                          |

Added with the NestJS app (ITACR-26):

| Script                              | Description                     |
| ----------------------------------- | ------------------------------- |
| `npm run start:dev`                 | Start the API in watch mode     |
| `npm run build`                     | Compile to `dist/`              |
| `npm run start:prod`                | Run the compiled API            |
| `npm test`                          | Unit tests                      |
| `npm run test:e2e`                  | End-to-end tests                |
| `npm run lint` / `npm run lint:fix` | Lint (and auto-fix) with ESLint |

## Folder structure

```
itacr-backend/
├── .github/            # CI and release-please workflows, Dependabot, PR and issue templates, CODEOWNERS
├── .husky/             # Git hooks: pre-commit (lint-staged), commit-msg (commitlint)
├── database/           # SQL objects that Prisma does not model, one object per file
│   ├── procedures/     # sp_*.sql
│   ├── functions/      # fn_*.sql
│   ├── views/          # vw_*.sql
│   ├── triggers/       # trg_*.sql
│   └── scripts/        # One-off maintenance or reporting scripts
├── docker/
│   └── docker-compose.yml   # PostgreSQL 18 and optional pgAdmin
├── docs/
│   └── CODING_STANDARDS.md
├── prisma/             # schema.prisma, migrations/, seed.ts (ITACR-27, ITACR-28)
├── src/                # NestJS application (ITACR-26), one module per feature:
│                       # auth, catalog, orders, kitchen, notifications, manager, health, prisma
├── .env.example
├── release-please-config.json
└── package.json
```

**Prisma is the source of truth for tables.** Procedures, functions, views, and triggers live in `database/` and reach the database only through Prisma migrations. See [database/README.md](database/README.md) and [section 4 of the coding standards](docs/CODING_STANDARDS.md#4-database-postgresql--prisma).

## Environment variables

Copy [.env.example](.env.example) to `.env`. Never commit `.env`.

| Variable                                                             | Purpose                                               |
| -------------------------------------------------------------------- | ----------------------------------------------------- |
| `NODE_ENV`, `PORT`                                                   | Runtime mode and HTTP port (3000)                     |
| `CORS_ORIGINS`                                                       | Allowed frontend origins, comma-separated             |
| `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `POSTGRES_PORT` | Local PostgreSQL container                            |
| `DATABASE_URL`                                                       | Prisma connection string                              |
| `PGADMIN_DEFAULT_EMAIL`, `PGADMIN_DEFAULT_PASSWORD`, `PGADMIN_PORT`  | Optional pgAdmin container                            |
| `JWT_SECRET`, `JWT_EXPIRES_IN`                                       | Authentication tokens                                 |
| `VAPID_PUBLIC_KEY`, `VAPID_PRIVATE_KEY`, `VAPID_SUBJECT`             | Web Push (the public key also goes to itacr-frontend) |
| `THROTTLE_TTL_MS`, `THROTTLE_LIMIT`                                  | Rate limiting                                         |

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
- Every Friday: `sprint-XX` → `develop` → `release` (QA) → `main`. release-please then creates the version tag (`v0.1.0`, `v0.2.0`, ...), `CHANGELOG.md`, and the GitHub Release on `main` (it never runs on or targets other branches), and `main` is back-merged into `release`.
- Hotfixes: `hotfix/ITACR-<N>-<description>` from `main`, PR into `main`, then the same branch into `develop`.
- CI validates that each PR comes from an allowed branch and prints the allowed flow when it does not.

Full workflow and sprint close checklist: [CONTRIBUTING.md](CONTRIBUTING.md).

## Commit convention

```
type(scope): short description (ITACR-N)
feat(orders): add idempotent order creation (ITACR-9)
```

Enforced by commitlint. Types, scopes, and details are in [CONTRIBUTING.md](CONTRIBUTING.md#3-commit-convention).

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

- [CONTRIBUTING.md](CONTRIBUTING.md): daily workflow, sprints, reviews, database workflow, bootstrapping, Definition of Done
- [docs/CODING_STANDARDS.md](docs/CODING_STANDARDS.md)
- [database/README.md](database/README.md)
- [CHANGELOG.md](CHANGELOG.md) (generated by release-please)
- [SECURITY.md](SECURITY.md)
