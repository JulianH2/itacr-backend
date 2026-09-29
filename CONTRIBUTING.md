# Contributing to itacr-backend

This guide describes how the ITACR team works day to day. Read it together with [docs/CODING_STANDARDS.md](docs/CODING_STANDARDS.md).

All commands work in PowerShell, Git Bash, and macOS/Linux terminals, one line at a time. The frontend repository, [itacr-frontend](https://github.com/JulianH2/itacr-frontend), follows the same workflow.

## Table of contents

1. [One-time setup](#1-one-time-setup)
2. [Branching model](#2-branching-model)
3. [Commit convention](#3-commit-convention)
4. [Daily workflow](#4-daily-workflow)
5. [Keeping your branch up to date and resolving conflicts](#5-keeping-your-branch-up-to-date-and-resolving-conflicts)
6. [Pull Requests and code review](#6-pull-requests-and-code-review)
7. [Starting a sprint](#7-starting-a-sprint-monday)
8. [Closing a sprint](#8-closing-a-sprint-friday)
9. [Hotfixes](#9-hotfixes)
10. [Releases and versioning](#10-releases-and-versioning)
11. [Scrum Master responsibilities](#11-scrum-master-responsibilities)
12. [Definition of Done](#12-definition-of-done)
13. [Database workflow](#13-database-workflow)
14. [Bootstrapping the backend (ITACR-26, ITACR-27, ITACR-28)](#14-bootstrapping-the-backend-itacr-26-itacr-27-itacr-28)

---

## 1. One-time setup

```bash
git clone https://github.com/JulianH2/itacr-backend.git
cd itacr-backend
npm ci                         # installs dependencies and activates the Husky git hooks
git config pull.rebase true    # pulling never creates accidental merge commits
git config user.name "Your Name"
git config user.email "you@example.com"   # use the email linked to your GitHub account
cp .env.example .env           # PowerShell: Copy-Item .env.example .env
npm run db:up                  # PostgreSQL in Docker on localhost:5432
```

Edit `.env` and replace every `change-me` value. `develop` is the default branch, so the clone starts there. Use the Node.js version in `.nvmrc` (24 LTS) and have Docker Desktop running. Install the VS Code extensions recommended in `.vscode/extensions.json` (ESLint, Prettier, EditorConfig, Prisma, Docker).

## 2. Branching model

```
personal branches                    sprint            integration     QA/staging      production

feature/ITACR-7-pin-login --+
fix/ITACR-9-order-total ----+--PR--> sprint-01 --PR--> develop --PR--> release --PR--> main --> vX.Y.Z tag
docs/ITACR-29-readme -------+                             ^               ^             |      (release-please)
                                                          |               |             |
                                                          +---------------+-back-merge--+

hotfix/ITACR-N-description (from main) --PR--> main, then back-merge main into release and develop
```

### 2.1 Branches

| Branch                           | Role                                                                                   | Created from | Receives PRs from                            |
| -------------------------------- | -------------------------------------------------------------------------------------- | ------------ | -------------------------------------------- |
| `main`                           | Production. release-please creates the version tag, `CHANGELOG.md`, and GitHub Release | -            | `release`, `hotfix/*`, the release-please PR |
| `release`                        | Staging / QA. The sprint increment is tested here before production                    | `main`       | `develop`, `main` (back-merge)               |
| `develop`                        | Integration. **Default branch** on GitHub                                              | `main`       | `sprint-XX`, `main` (back-merge), Dependabot |
| `sprint-XX`                      | One per weekly sprint (`sprint-01`, `sprint-02`, ...). Nobody commits to it directly   | `develop`    | Personal branches only                       |
| `<type>/ITACR-<N>-<description>` | Personal work branch, one per Jira issue. Deleted after merge                          | `sprint-XX`  | -                                            |
| `hotfix/ITACR-<N>-<description>` | Urgent fix for production                                                              | `main`       | -                                            |

### 2.2 Protection rules

`main`, `release`, and `develop` are protected (the Scrum Master can also protect `sprint-*`):

- Pull Request required, with **1 approval** from the other team member.
- Required status checks: **`ci`** and **`branch-name`**.
- No direct pushes and no force pushes, for everyone (including admins).

### 2.3 Personal branch names

```
<type>/ITACR-<N>-<short-kebab-description>
```

- `type`: `feature`, `fix`, `docs`, `refactor`, `test`, or `chore`.
- Examples: `feature/ITACR-26-nestjs-scaffold`, `feature/ITACR-27-database-schema`, `fix/ITACR-9-order-total`.
- Lowercase kebab-case after the key: letters, numbers, and single hyphens only.
- The `branch-name` CI job rejects PRs whose source branch does not match the flow above.

## 3. Commit convention

[Conventional Commits](https://www.conventionalcommits.org/) in English, imperative mood, lowercase, no final period, with the Jira key at the end:

```
type(scope): short description (ITACR-N)
```

Examples:

```
feat(orders): add idempotent order creation (ITACR-9)
feat(db): add orders and order items tables (ITACR-27)
fix(kitchen): emit status updates to the right room (ITACR-12)
chore(deps): bump @nestjs/core to 11.2.0
```

| Types                                                                              | Scopes                                                                                   |
| ---------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `feat` `fix` `docs` `style` `refactor` `perf` `test` `build` `ci` `chore` `revert` | `auth` `catalog` `orders` `kitchen` `tracking` `manager` `pwa` `db` `core` `deps` `repo` |

- Use the `db` scope for Prisma schema, migrations, seeds, and SQL objects.
- The Husky `commit-msg` hook runs **commitlint** and rejects messages that do not follow the format. Only the `deps` scope may omit the Jira key.
- The Husky `pre-commit` hook runs **lint-staged**: ESLint `--fix` and Prettier on staged files, and `prisma format` on `.prisma` files.
- The Jira key in the commit, the branch name, and the PR title links everything to the Jira issue automatically (GitHub for Jira integration).
- Commit types drive the version and the changelog (see [section 10](#10-releases-and-versioning)), so choose them carefully: `feat` is a new feature for users, `fix` is a bug fix.

## 4. Daily workflow

The examples use `sprint-01` and the issue `ITACR-9`.

### 4.1 Start an issue: create your personal branch

From the terminal:

```bash
git switch sprint-01
git pull
git switch -c feature/ITACR-9-create-order
```

Or from Jira: open the issue, click **Create branch**, choose the `itacr-backend` repository, set the **base branch to `sprint-01`**, and edit the name to follow `<type>/ITACR-<N>-<description>`. Then:

```bash
git fetch origin
git switch feature/ITACR-9-create-order
```

Move the Jira issue to **In Progress**.

### 4.2 Work and commit

```bash
npm run db:up                      # make sure PostgreSQL is running
npx prisma migrate dev             # apply any migration your teammate merged
npm run start:dev                  # http://localhost:3000/api/v1
git status
git add src/orders                 # stage specific paths; avoid "git add ." with unknown files
git commit -m "feat(orders): add idempotent order creation (ITACR-9)"
```

If commitlint rejects the message, fix it and commit again. Your staged changes are kept.

### 4.3 Push (at least once a day)

```bash
npm test
npm run build
git push -u origin feature/ITACR-9-create-order     # -u only the first time; later just "git push"
```

### 4.4 Open the PR into the sprint branch

When the issue is ready, open a PR from your branch into `sprint-01` (see [6.1](#61-opening-a-pr)) and move the Jira issue to **In Review**.

### 4.5 After the merge

Click **Delete branch** on the merged PR in GitHub, then clean up locally:

```bash
git switch sprint-01
git pull
git branch -d feature/ITACR-9-create-order
git fetch --prune
```

## 5. Keeping your branch up to date and resolving conflicts

### 5.1 Bring the latest sprint changes into your branch

Do this when your teammate merged something you need, or when GitHub says your PR has conflicts:

```bash
git switch feature/ITACR-9-create-order
git fetch origin
git merge origin/sprint-01
npx prisma migrate dev             # apply migrations that came with the merge
```

If there are conflicts:

1. Open the conflicted files in VS Code and choose the correct result (often both changes).
2. Remove every `<<<<<<<`, `=======`, `>>>>>>>` marker.
3. Finish the merge and push:

```bash
git add src/orders/orders.service.ts
git commit                         # accept the default merge message
npm test
git push
```

To give up and go back to the state before the merge: `git merge --abort`.

If `package-lock.json` conflicts, take the sprint version and regenerate it with your dependencies:

```bash
git checkout --theirs package-lock.json
npm install
git add package-lock.json
```

If `prisma/schema.prisma` conflicts, keep both sets of changes, then create a new migration for your part (`npx prisma migrate dev --name <change>`). Never edit or renumber a migration that came from your teammate.

### 5.2 Rules

- **Never** use `git push --force` on `sprint-*`, `develop`, `release`, or `main` (protection blocks it anyway).
- Merge the sprint branch into your personal branch; do not rebase a personal branch that is already pushed and under review.

## 6. Pull Requests and code review

### 6.1 Opening a PR

| From                           | To                   | Compare URL                                                                                  | When                       |
| ------------------------------ | -------------------- | -------------------------------------------------------------------------------------------- | -------------------------- |
| `feature/ITACR-9-create-order` | `sprint-01`          | `https://github.com/JulianH2/itacr-backend/compare/sprint-01...feature/ITACR-9-create-order` | Issue finished             |
| `sprint-01`                    | `develop`            | `https://github.com/JulianH2/itacr-backend/compare/develop...sprint-01`                      | Sprint close               |
| `develop`                      | `release`            | `https://github.com/JulianH2/itacr-backend/compare/release...develop`                        | Sprint close               |
| `release`                      | `main`               | `https://github.com/JulianH2/itacr-backend/compare/main...release`                           | Sprint close, after QA     |
| `hotfix/ITACR-N-...`           | `main`               | `https://github.com/JulianH2/itacr-backend/compare/main...hotfix/ITACR-N-...`                | Urgent production fix      |
| `main`                         | `release`, `develop` | `.../compare/release...main`, `.../compare/develop...main`                                   | Back-merge after a release |

- **Personal PR title**: a Conventional Commit with the key, e.g. `feat(orders): add idempotent order creation (ITACR-9)`.
- **Flow PR title**: `Sprint 01: sprint-01 into develop`, `Sprint 01: develop into release`, `Release: release into main`.
- Fill in every section of the PR template. The other team member is requested automatically through `CODEOWNERS`.

### 6.2 Reviewing a PR

1. Read the description and the linked Jira issue.
2. Check out the PR branch and run it:

   ```bash
   git fetch origin
   git switch feature/ITACR-9-create-order
   npm ci
   npm run db:up
   npx prisma migrate dev
   npm run lint
   npm test
   npm run build
   npm run start:dev
   ```

3. Test the acceptance criteria with Swagger (`http://localhost:3000/api/docs`) or curl, including invalid input (validation errors) and a user without the required role (`403`).
4. For database changes, check the migration SQL and that every SQL object has its file in `database/` (see [section 13](#13-database-workflow)).
5. Review the code against the coding standards. Leave comments on **Files changed**. Prefix optional remarks with `nit:`.
6. Submit **Approve** or **Request changes**. Do not approve with failing CI.

### 6.3 Merging

- Merge method: **Create a merge commit**, for every PR. Squash or rebase merges on long-lived branches rewrite commits, which makes `develop`, `release`, and `main` diverge and causes false conflicts at the next sprint close.
- Only merge after 1 approval and green `ci` and `branch-name` checks.
- Delete personal and hotfix branches after merge. Never delete `develop`, `release`, or `main`.

## 7. Starting a sprint (Monday)

The Scrum Master of the new sprint creates the branch from the updated `develop`:

```bash
git switch develop
git pull
git switch -c sprint-02
git push -u origin sprint-02
```

The other team member then runs:

```bash
git fetch origin
git switch sprint-02
```

## 8. Closing a sprint (Friday)

The Scrum Master runs the close; the other team member approves each PR. Replace `01` with the current sprint number.

- [ ] **1. All personal PRs are merged into `sprint-01`.** Check that `is:pr is:open base:sprint-01` returns nothing in the GitHub PR list, then verify the branch locally:

  ```bash
  git fetch origin --prune
  git switch sprint-01
  git pull
  npm ci
  npm run lint
  npm test
  npm run build
  ```

- [ ] **2. PR `sprint-01` → `develop`.** Open `https://github.com/JulianH2/itacr-backend/compare/develop...sprint-01`, get approval and green checks, and merge with a merge commit.

- [ ] **3. PR `develop` → `release`, then QA on `release`.** Open `https://github.com/JulianH2/itacr-backend/compare/release...develop` and merge it. Then test the increment against a clean database, the same way production applies migrations:

  ```bash
  git fetch origin
  git switch release
  git pull
  npm ci
  docker compose -f docker/docker-compose.yml --env-file .env down -v
  npm run db:up
  npx prisma migrate deploy
  npx prisma db seed
  npm run build
  npm run start:prod
  ```

  Smoke test: `GET http://localhost:3000/api/v1/health` returns `200`, Swagger loads, and the sprint's endpoints work with the frontend. If you find a blocking bug, fix it with a personal branch into `sprint-01` and repeat steps 2 and 3. Do not go on to step 4 until QA passes.

- [ ] **4. PR `release` → `main`.** Open `https://github.com/JulianH2/itacr-backend/compare/main...release`, get approval, and merge.

- [ ] **5. Merge the release-please PR.** A few seconds after step 4, release-please opens (or updates) the PR `chore(repo): release vX.Y.Z` on `main`. Review the version and the generated `CHANGELOG.md`, approve, and merge. release-please then creates the tag `vX.Y.Z` and the GitHub Release.

- [ ] **6. Back-merge `main` into `release` and `develop`**, so the version bump and `CHANGELOG.md` are in sync. The branches are protected, so this is done with two PRs:

  ```bash
  git fetch origin --tags
  git log --oneline origin/release..origin/main    # what the back-merge brings into release
  git log --oneline origin/develop..origin/main    # what the back-merge brings into develop
  ```

  Open and merge (merge commit) both PRs:
  - `https://github.com/JulianH2/itacr-backend/compare/release...main`
  - `https://github.com/JulianH2/itacr-backend/compare/develop...main`

  Then update your local copies:

  ```bash
  git switch main
  git pull
  git switch release
  git pull
  git switch develop
  git pull
  git tag --list "v*"                              # the new tag must appear
  ```

  If a back-merge PR has conflicts (rare; only when `develop` changed the same lines), resolve them in a sync branch, which is the only other source allowed by the `branch-name` check:

  ```bash
  git switch -c sync/main-into-develop origin/develop
  git merge origin/main
  # resolve the conflicts, then:
  git add .
  git commit
  git push -u origin sync/main-into-develop
  ```

  Open a PR `sync/main-into-develop` → `develop`, merge it, and delete the branch. Use `sync/main-into-release` from `origin/release` for `release`.

- [ ] **7. On Monday, create `sprint-02` from `develop`** (see [section 7](#7-starting-a-sprint-monday)).

Keep the old `sprint-XX` branches: they are evidence of each sprint's work for the course. Nobody commits to them after the close.

## 9. Hotfixes

Only for urgent bugs in production (`main`). The example uses `ITACR-35`:

```bash
git switch main
git pull
git switch -c hotfix/ITACR-35-duplicate-orders-on-sync
# fix + test
git commit -m "fix(orders): reject duplicate idempotency keys (ITACR-35)"
git push -u origin hotfix/ITACR-35-duplicate-orders-on-sync
```

1. Open a PR `hotfix/ITACR-35-duplicate-orders-on-sync` → `main`, get 1 approval and green checks, and merge it.
2. release-please opens a patch release PR (for example `v0.1.1`). Merge it.
3. Back-merge `main` into `release` and `develop`, exactly like [step 6 of the sprint close](#8-closing-a-sprint-friday).
4. Delete the hotfix branch.

The current `sprint-XX` receives the fix at the next sprint close. If someone needs it earlier, they merge `origin/develop` into their personal branch ([5.1](#51-bring-the-latest-sprint-changes-into-your-branch), with `origin/develop` instead of `origin/sprint-01`). If the hotfix includes a migration, it must be a new migration, never an edit of an existing one.

## 10. Releases and versioning

Releases are automated with [release-please](https://github.com/googleapis/release-please) (`.github/workflows/release-please.yml`), which runs on every push to `main`:

- It reads the Conventional Commits merged into `main` since the last release, and opens or updates a PR `chore(repo): release vX.Y.Z` with the new version in `package.json` and `.release-please-manifest.json`, and the new `CHANGELOG.md` section.
- Merging that PR creates the tag `vX.Y.Z` and the GitHub Release. The first release is `v0.1.0` (`initial-version` in `release-please-config.json`).
- **Never edit `CHANGELOG.md` or the version by hand, and never create tags manually.**

Version rules (Semantic Versioning, while below 1.0.0):

| Commits since the last release | Bump  | Example         |
| ------------------------------ | ----- | --------------- |
| At least one `feat`            | minor | `0.1.0 → 0.2.0` |
| Only `fix`, `perf`, etc.       | patch | `0.2.0 → 0.2.1` |
| Breaking change (`feat!:`)     | minor | `0.2.1 → 0.3.0` |

A normal sprint therefore produces `v0.1.0`, `v0.2.0`, and so on. For the final release, add the footer `Release-As: 1.0.0` to a commit body on the last sprint:

```bash
git commit -m "chore(repo): prepare final release (ITACR-N)" -m "Release-As: 1.0.0"
```

**One-time setup (repository owner):** release-please must open its PR with a personal access token, because PRs created with the default `GITHUB_TOKEN` do not trigger CI and the required checks would never pass.

1. GitHub > Settings > Developer settings > **Fine-grained tokens** > Generate new token. Repository access: `itacr-backend` (one token can cover both repositories). Permissions: **Contents: Read and write**, **Pull requests: Read and write**.
2. In the repository: Settings > Secrets and variables > Actions > **New repository secret**, named `RELEASE_PLEASE_TOKEN`.

## 11. Scrum Master responsibilities

The Scrum Master role rotates every sprint. Regarding the repository, the Scrum Master:

- Creates `sprint-XX` from `develop` on Monday and announces it.
- Makes sure personal PRs are reviewed within one working day and that CI failures are fixed quickly.
- Reviews and merges the weekly Dependabot PRs into `develop` (or assigns them).
- On Friday, runs the [sprint close checklist](#8-closing-a-sprint-friday) up to the release and the back-merges.
- Makes sure every Jira issue in the sprint is in the correct column and that closed issues meet the Definition of Done.

## 12. Definition of Done

An issue is **Done** only when all of the following are true:

- [ ] The acceptance criteria in Jira are met and were tested by the other team member.
- [ ] The code was reviewed and approved in a PR into `sprint-XX`, and the personal branch was deleted.
- [ ] CI is green (`branch-name`, and `ci`: format check, Prisma validation, lint, tests, build).
- [ ] It follows [docs/CODING_STANDARDS.md](docs/CODING_STANDARDS.md), with at least one test per acceptance criterion when feasible.
- [ ] Every new endpoint has DTO validation and Swagger documentation.
- [ ] Database changes are in migrations (and SQL objects in `database/`), and the seeds still run twice without errors.
- [ ] Docs are updated (README, `.env.example`) if applicable.
- [ ] The Jira issue is moved to **Done**.

## 13. Database workflow

The database is part of this repository and changes only through Prisma migrations. **Never change the database by hand** (pgAdmin, psql) except for reading data.

| What                                   | Where                        | How it reaches the database                          |
| -------------------------------------- | ---------------------------- | ---------------------------------------------------- |
| Tables, enums, relations, indexes      | `prisma/schema.prisma`       | `npx prisma migrate dev --name <change>`             |
| Procedures, functions, views, triggers | `database/<type>/<name>.sql` | Copied into a migration created with `--create-only` |
| Seed data                              | `prisma/seed.ts`             | `npx prisma db seed` (idempotent)                    |
| Reports and maintenance                | `database/scripts/`          | Run manually; never change schema or shared data     |

### 13.1 Change a table

```bash
# edit prisma/schema.prisma
npx prisma migrate dev --name add_orders_notes_column
git add prisma/schema.prisma prisma/migrations
git commit -m "feat(db): add notes column to orders (ITACR-N)"
```

### 13.2 Add or change a procedure, function, view, or trigger

```bash
# 1. write or update database/functions/fn_get_orders_by_status.sql (with the header comment)
npx prisma migrate dev --create-only --name add_fn_get_orders_by_status
# 2. copy the full SQL file into prisma/migrations/<timestamp>_add_fn_get_orders_by_status/migration.sql
npx prisma migrate dev
git add database/functions/fn_get_orders_by_status.sql prisma/migrations
git commit -m "feat(db): add fn_get_orders_by_status (ITACR-N)"
```

Naming, headers, `CREATE OR REPLACE`, and the procedure-vs-function rule are in [section 4.2 of the coding standards](docs/CODING_STANDARDS.md#42-sql-objects-procedures-functions-views-and-triggers) and in [database/README.md](database/README.md).

### 13.3 Reset your local database

```bash
npx prisma migrate reset           # drops the local database, reapplies every migration, and runs the seed
```

Only ever run this against your local Docker database.

## 14. Bootstrapping the backend (ITACR-26, ITACR-27, ITACR-28)

The repository starts with configuration and empty folders only. These three issues create the application and the database, each in its own personal branch from `sprint-01` and its own PR into `sprint-01`.

### 14.1 NestJS app (ITACR-26)

The repository already has `package.json`, `.prettierrc`, `.gitignore`, and `README.md`, so the Nest CLI cannot generate directly inside it without overwriting them. Generate the app in a temporary sibling folder and copy only what is needed.

```bash
git switch sprint-01
git pull
git switch -c feature/ITACR-26-nestjs-scaffold

# 1. Generate a strict NestJS project next to the repository
cd ..
npx @nestjs/cli@latest new itacr-backend-scaffold --package-manager npm --strict --skip-git --skip-install
cd itacr-backend
```

2. Copy these from `../itacr-backend-scaffold` into the repository (and delete `src/.gitkeep`):
   - `src/` (contents), `test/`, `nest-cli.json`, `tsconfig.json`, `tsconfig.build.json`, `eslint.config.mjs`
   - **Do not copy** `package.json`, `package-lock.json`, `.prettierrc`, `.gitignore`, `README.md`, or `node_modules/`.
3. Merge the scaffold's `package.json` into ours:
   - Copy its `dependencies`, `devDependencies`, and `jest` blocks. Keep our existing dev dependencies (Husky, lint-staged, commitlint, Prettier).
   - Copy its scripts except `format`, and keep ours (`format`, `format:check`, `db:*`, `prepare`).
   - Replace its lint script so CI never modifies files, and add `lint:fix`:

     ```json
     "lint": "eslint \"{src,test}/**/*.ts\"",
     "lint:fix": "eslint \"{src,test}/**/*.ts\" --fix",
     ```

4. In `eslint.config.mjs`, set these rules (the scaffold turns some of them off):

   ```js
   rules: {
     '@typescript-eslint/no-explicit-any': 'error',
     '@typescript-eslint/explicit-function-return-type': ['error', { allowExpressions: true, allowTypedFunctionExpressions: true }],
     '@typescript-eslint/no-floating-promises': 'error',
     'no-console': 'error',
     eqeqeq: ['error', 'always'],
   },
   ```

5. Install, delete the temporary folder, and verify:

   ```bash
   npm install
   npm run lint
   npm run format
   npm test
   npm run build
   ```

   Delete `../itacr-backend-scaffold` (PowerShell: `Remove-Item -Recurse -Force ..\itacr-backend-scaffold`).

6. As part of ITACR-26, configure the base app following [section 3 of the coding standards](docs/CODING_STANDARDS.md#3-nestjs-itacr-backend): `@nestjs/config` with validation of every variable in `.env.example`, global prefix `api/v1`, global `ValidationPipe`, `helmet`, CORS from `CORS_ORIGINS`, `@nestjs/throttler`, Swagger at `/api/docs`, and `GET /api/v1/health`.

7. Commit and open the PR into `sprint-01`:

   ```bash
   git add .
   git status                       # check that no .env or node_modules is staged
   git commit -m "feat(core): scaffold nestjs application with base configuration (ITACR-26)"
   git push -u origin feature/ITACR-26-nestjs-scaffold
   ```

From this PR on, CI detects `nest-cli.json` and runs lint, tests, and build.

### 14.2 Prisma and the database schema (ITACR-27)

Start from `sprint-01` after ITACR-26 is merged:

```bash
git switch sprint-01
git pull
git switch -c feature/ITACR-27-database-schema
npm run db:up
npm install @prisma/client
npm install -D prisma
npx prisma init --datasource-provider postgresql
```

Then:

1. Delete `prisma/.gitkeep`.
2. `prisma init` may create or change `.env` and `.gitignore`. Keep our `.gitignore` (it already ignores `.env` and `src/generated`), and make sure `.env` has the `DATABASE_URL` from `.env.example`.
3. If the generator has an `output` path, point it to `../src/generated/prisma` (already git-ignored).
4. Model the tables following [section 4.1 of the coding standards](docs/CODING_STANDARDS.md#41-tables-with-prisma): PascalCase singular models, `@@map` to snake_case plural tables, `@map` on columns, `created_at`/`updated_at`, indexes, `Decimal` for money, and `Json` for combo customizations.
5. Create the first migration and a `PrismaService` in `src/prisma/`:

   ```bash
   npx prisma migrate dev --name init
   npx prisma validate
   ```

6. Add any procedure, function, view, or trigger with the workflow in [section 13.2](#132-add-or-change-a-procedure-function-view-or-trigger).
7. Commit with the `db` scope and open the PR into `sprint-01`:

   ```bash
   git add prisma src/prisma package.json package-lock.json database
   git commit -m "feat(db): add initial database schema (ITACR-27)"
   git push -u origin feature/ITACR-27-database-schema
   ```

### 14.3 Seed data (ITACR-28)

Branch `feature/ITACR-28-seed-data` from `sprint-01` after ITACR-27 is merged.

1. Write `prisma/seed.ts` using only `upsert` (or `createMany` with `skipDuplicates`), so it can run any number of times. Hash every seeded PIN or password with bcrypt or argon2.
2. Register the seed command as the installed Prisma version requires: `"prisma": { "seed": "..." }` in `package.json` for Prisma 6, or `migrations.seed` in `prisma.config.ts` for Prisma 7 and later.
3. Verify idempotency by running it twice:

   ```bash
   npx prisma migrate reset
   npx prisma db seed
   npx prisma db seed
   ```

4. Commit `feat(db): add idempotent seed data (ITACR-28)` and open the PR into `sprint-01`.
