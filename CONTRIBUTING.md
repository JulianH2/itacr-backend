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
15. [Deployment to Azure](#15-deployment-to-azure)

---

## 1. One-time setup

Install the **.NET SDK 10** (`dotnet --version` must print `10.0.x`, see `global.json`) and **Docker Desktop**, and keep Docker running while you work.

```bash
git clone https://github.com/JulianH2/itacr-backend.git
cd itacr-backend
git config core.hooksPath .githooks       # activates the commit-msg and pre-commit hooks
git config pull.rebase true               # pulling never creates accidental merge commits
git config user.name "Your Name"
git config user.email "you@example.com"   # use the email linked to your GitHub account
cp .env.example .env                      # PowerShell: Copy-Item .env.example .env
```

Edit `.env` and set a strong `MSSQL_SA_PASSWORD` (at least 8 characters with uppercase, lowercase, numbers, and symbols; SQL Server stops the container if it is weak). Then start the database and restore the local tools (`dotnet-ef`):

```bash
docker compose up -d sqlserver
docker compose ps                         # wait until itacr-sqlserver is "healthy"
dotnet tool restore
```

Store the API and migrator secrets with [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets). They live in your user profile, outside the repository, and are only loaded in Development:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=Itacr;User Id=sa;Password=<MSSQL_SA_PASSWORD from .env>;TrustServerCertificate=True" --project src/Itacr.Api
dotnet user-secrets set "ConnectionStrings:Migrator" "Server=localhost,1433;Database=Itacr;User Id=sa;Password=<MSSQL_SA_PASSWORD from .env>;TrustServerCertificate=True" --project database/Itacr.Database.Migrator
dotnet user-secrets set "Jwt:Secret" "<random value, at least 64 characters>" --project src/Itacr.Api
dotnet user-secrets list --project src/Itacr.Api
```

Generate the JWT secret with `node -e "console.log(require('crypto').randomBytes(48).toString('base64url'))"` or, in Git Bash, `openssl rand -base64 48`. Generate the Web Push keys once for the team with `npx web-push generate-vapid-keys`: the private key goes to `dotnet user-secrets set "WebPush:PrivateKey" "<key>"`, and the public key goes to `appsettings.json` (`WebPush:PublicKey`) and to `itacr-frontend`.

Finally, create the database with the migrator (schema, catalog, and the four test employees) and run the API:

```bash
dotnet run --project database/Itacr.Database.Migrator
dotnet run --project src/Itacr.Api               # http://localhost:3000/api/v1
```

`develop` is the default branch, so the clone starts there. Install the VS Code extensions recommended in `.vscode/extensions.json` (C# Dev Kit, EditorConfig, Docker, SQL Server, REST Client), or use Visual Studio or Rider. To browse the local database, connect the SQL Server extension to `localhost,1433` with the `sa` user and "Trust server certificate".

> Until ITACR-26 creates `src/Itacr.Api`, `compose.yaml`, and the tool manifest, skip everything after `git config` except copying `.env`.

## 2. Branching model

```
personal branches                    sprint            integration     QA/staging      production

feature/ITACR-7-pin-login --+
fix/ITACR-9-order-total ----+--PR--> sprint-01 --PR--> develop --PR--> release --PR--> main --> vX.Y.Z tag
docs/ITACR-29-readme -------+                                             ^             |      (release-please)
                                                                          |             |
                                                                          +-back-merge--+

hotfix/ITACR-N-description (from main) --PR--> main, then the same branch --PR--> develop
```

### 2.1 Branches

| Branch                           | Role                                                                                   | Created from | Receives PRs from                                |
| -------------------------------- | -------------------------------------------------------------------------------------- | ------------ | ------------------------------------------------ |
| `main`                           | Production. release-please creates the version tag, `CHANGELOG.md`, and GitHub Release | -            | `release`, `hotfix/*`, the release-please PR     |
| `release`                        | Staging / QA. The sprint increment is tested here before production                    | `main`       | `develop`, `main` (back-merge)                   |
| `develop`                        | Integration. **Default branch** on GitHub                                              | `main`       | `sprint-XX`, `hotfix/*` (back-merge), Dependabot |
| `sprint-XX`                      | One per weekly sprint (`sprint-01`, `sprint-02`, ...). Nobody commits to it directly   | `develop`    | Personal branches only                           |
| `<type>/ITACR-<N>-<description>` | Personal work branch, one per Jira issue. Deleted after merge                          | `sprint-XX`  | -                                                |
| `hotfix/ITACR-<N>-<description>` | Urgent fix for production                                                              | `main`       | -                                                |

### 2.2 Protection rules

`main`, `release`, and `develop` are protected (the Scrum Master can also protect `sprint-*`):

- Pull Request required, with **1 approval** from the other team member.
- Required status checks: **`ci`** and **`branch-name`**.
- No direct pushes and no force pushes, for everyone (including admins).

### 2.3 Personal branch names

```
<type>/ITACR-<N>-<short-kebab-description>
```

- `type`: `feature`, `fix`, `docs`, `refactor`, `test`, or `chore`. Technical tasks that add something new (scaffolding, schema, seed data) are `feature`; maintenance and configuration are `chore`.
- Examples: `feature/ITACR-26-api-base-and-db-connection`, `feature/ITACR-27-database-schema`, `fix/ITACR-9-order-total`.
- Lowercase kebab-case after the key: letters, numbers, and single hyphens only.
- **One branch and one PR per Jira issue.** Never put several issues in one branch, and never create a branch from another personal branch: always from the current `sprint-XX`.
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
fix(kitchen): notify the right signalr group on status change (ITACR-12)
chore(deps): bump microsoft.entityframeworkcore to 10.0.4
```

| Types                                                                              | Scopes                                                                                   |
| ---------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `feat` `fix` `docs` `style` `refactor` `perf` `test` `build` `ci` `chore` `revert` | `auth` `catalog` `orders` `kitchen` `tracking` `manager` `pwa` `db` `core` `deps` `repo` |

- Use the `db` scope for the EF Core model, migrations, seed data, and SQL objects.
- The `commit-msg` git hook (`.githooks/commit-msg`) rejects messages that do not follow the format, and CI runs the same check on every commit of a PR into `sprint-XX`. Only the `deps` scope may omit the Jira key. The rules are the same as commitlint in `itacr-frontend`.
- The `pre-commit` hook runs `dotnet format` on the staged C# files and stages the result. Stage whole files, not parts of a file.
- If the hooks do not run, check `git config core.hooksPath` prints `.githooks` (see [section 1](#1-one-time-setup)).
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
docker compose up -d sqlserver                       # make sure SQL Server is running
dotnet run --project database/Itacr.Database.Migrator             # apply any database script your teammate merged
dotnet watch --project src/Itacr.Api                 # http://localhost:3000/api/v1, reloads on save
git status
git add src/Itacr.Application/Orders src/Itacr.Api/Endpoints/Orders  # stage specific paths; avoid "git add ." with unknown files
git commit -m "feat(orders): add idempotent order creation (ITACR-9)"
```

If the commit-msg hook rejects the message, fix it and commit again. Your staged changes are kept.

### 4.3 Push (at least once a day)

```bash
dotnet format --verify-no-changes
dotnet build -c Release
dotnet test
git push -u origin feature/ITACR-9-create-order     # -u only the first time; later just "git push"
```

`dotnet build -c Release` is what CI runs: every warning is an error there.

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
dotnet run --project database/Itacr.Database.Migrator             # apply database scripts that came with the merge
```

If there are conflicts:

1. Open the conflicted files in VS Code and choose the correct result (often both changes).
2. Remove every `<<<<<<<`, `=======`, `>>>>>>>` marker.
3. Finish the merge and push:

```bash
git add src/Itacr.Application/Orders/OrdersService.cs
git commit                         # accept the default merge message
dotnet build
dotnet test
git push
```

To give up and go back to the state before the merge: `git merge --abort`.

If `Directory.Packages.props` conflicts, keep every `<PackageVersion>` from both sides, and for the same package keep the higher version. Then run `dotnet restore`.

### 5.2 Migration conflicts

If you and your teammate both added a script with the same number in `database/migrations/` (for example two `V002__...sql`), git does not report a conflict, but the migrator would run them in name order and the journal would mix them up. Whoever merges second renames their own script to the next free number before merging:

```bash
git mv database/migrations/V002__add_order_notes.sql database/migrations/V003__add_order_notes.sql
git commit -m "fix(db): renumber order notes migration (ITACR-N)"
```

Only rename a script that is not merged into `sprint-XX` yet. Then reset your local database ([section 13.3](#133-reset-your-local-database)) so both scripts run in the new order.

### 5.3 Rules

- **Never** use `git push --force` on `sprint-*`, `develop`, `release`, or `main` (protection blocks it anyway).
- Merge the sprint branch into your personal branch; do not rebase a personal branch that is already pushed and under review.

## 6. Pull Requests and code review

### 6.1 Opening a PR

| From                           | To          | Compare URL                                                                                  | When                          |
| ------------------------------ | ----------- | -------------------------------------------------------------------------------------------- | ----------------------------- |
| `feature/ITACR-9-create-order` | `sprint-01` | `https://github.com/JulianH2/itacr-backend/compare/sprint-01...feature/ITACR-9-create-order` | Issue finished                |
| `sprint-01`                    | `develop`   | `https://github.com/JulianH2/itacr-backend/compare/develop...sprint-01`                      | Sprint close                  |
| `develop`                      | `release`   | `https://github.com/JulianH2/itacr-backend/compare/release...develop`                        | Sprint close                  |
| `release`                      | `main`      | `https://github.com/JulianH2/itacr-backend/compare/main...release`                           | Sprint close, after QA        |
| `hotfix/ITACR-N-...`           | `main`      | `https://github.com/JulianH2/itacr-backend/compare/main...hotfix/ITACR-N-...`                | Urgent production fix         |
| `hotfix/ITACR-N-...`           | `develop`   | `https://github.com/JulianH2/itacr-backend/compare/develop...hotfix/ITACR-N-...`             | After the hotfix is in `main` |
| `main`                         | `release`   | `https://github.com/JulianH2/itacr-backend/compare/release...main`                           | Back-merge after a release    |

Any other combination is rejected by the `branch-name` CI check, which prints the allowed flow in its log. Before creating the PR, check that the **base** branch at the top of the page is the right one.

- **Personal PR title**: a Conventional Commit with the key, e.g. `feat(orders): add idempotent order creation (ITACR-9)`.
- **Flow PR title**: `Sprint 01: sprint-01 into develop`, `Sprint 01: develop into release`, `Release: release into main`.
- Fill in every section of the PR template. The other team member is requested automatically through `CODEOWNERS`.

### 6.2 Reviewing a PR

1. Read the description and the linked Jira issue.
2. Check out the PR branch and run it:

   ```bash
   git fetch origin
   git switch feature/ITACR-9-create-order
   docker compose up -d sqlserver
   dotnet run --project database/Itacr.Database.Migrator
   dotnet format --verify-no-changes
   dotnet build -c Release
   dotnet test
   dotnet run --project src/Itacr.Api
   ```

3. Test the acceptance criteria with Scalar (`http://localhost:3000/scalar/v1`), the `.http` files, or curl, including invalid input (`400` with Problem Details in Spanish) and a user without the required role (`403`).
4. For database changes, read every new script in `database/migrations/` (additive, never an edit of a merged script) and every changed object file, and check that the migrator runs twice without errors (see [section 13](#13-database-workflow)).
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
  dotnet restore
  dotnet format --verify-no-changes
  dotnet build -c Release
  dotnet test
  ```

- [ ] **2. PR `sprint-01` → `develop`.** Open `https://github.com/JulianH2/itacr-backend/compare/develop...sprint-01`, get approval and green checks, and merge with a merge commit.

- [ ] **3. PR `develop` → `release`, then QA on `release`.** Open `https://github.com/JulianH2/itacr-backend/compare/release...develop` and merge it. Then test the increment against a clean database, applying every script from scratch:

  ```bash
  git fetch origin
  git switch release
  git pull
  docker compose down -v
  docker compose up -d sqlserver
  dotnet run --project database/Itacr.Database.Migrator
  dotnet run --project src/Itacr.Api -c Release
  ```

  Smoke test: `GET http://localhost:3000/api/v1/health` returns `200`, Scalar loads, and the sprint's endpoints work with the frontend. If you find a blocking bug, fix it with a personal branch into `sprint-01` and repeat steps 2 and 3. Do not go on to step 4 until QA passes.

- [ ] **4. PR `release` → `main`, then production deploy.** Open `https://github.com/JulianH2/itacr-backend/compare/main...release`, get approval, and merge. The push to `main` starts the **Deploy** workflow, which applies the database scripts and then deploys the API ([section 15.2](#152-deploy-workflow)). When it finishes, check `GET https://app-itacr-backend-prod-b3hvcggncghafbgp.westus-01.azurewebsites.net/api/v1/health` returns `200 Healthy`. If the `migrate` job fails, the API is not deployed: read its log, fix the script with a personal branch, and release again.

- [ ] **5. Merge the release-please PR.** A few seconds after step 4, release-please opens (or updates) the PR `chore(repo): release vX.Y.Z` on `main`. Review the version and the generated `CHANGELOG.md`, approve, and merge. release-please then creates the tag `vX.Y.Z` and the GitHub Release.

- [ ] **6. Back-merge `main` into `release`**, so `release` has the version bump and `CHANGELOG.md` before the next sprint. The branch is protected, so this is done with a PR:

  ```bash
  git fetch origin --tags
  git log --oneline origin/release..origin/main    # what the back-merge brings into release
  ```

  Open `https://github.com/JulianH2/itacr-backend/compare/release...main` and merge it (merge commit). It cannot conflict: `release` was merged into `main` in step 4, so the only new commit is the release-please one.

  `develop` is not back-merged. It never edits `CHANGELOG.md` or the version, and release-please only reads `main`, so the next `develop` → `release` → `main` PRs stay conflict-free.

  Then update your local copies:

  ```bash
  git switch main
  git pull
  git switch release
  git pull
  git tag --list "v*"                              # the new tag must appear
  ```

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

1. Open a PR `hotfix/ITACR-35-duplicate-orders-on-sync` → `main`, get 1 approval and green checks, and merge it. **Do not delete the branch yet.**
2. release-please opens a patch release PR (for example `v0.1.1`). Merge it.
3. Open a second PR from **the same hotfix branch** into `develop` (`https://github.com/JulianH2/itacr-backend/compare/develop...hotfix/ITACR-35-duplicate-orders-on-sync`) and merge it. If it conflicts, bring `develop` into the hotfix branch first:

   ```bash
   git switch hotfix/ITACR-35-duplicate-orders-on-sync
   git fetch origin
   git merge origin/develop
   # resolve the conflicts, then:
   git add .
   git commit
   git push
   ```

4. Back-merge `main` into `release`, exactly like [step 6 of the sprint close](#8-closing-a-sprint-friday).
5. Delete the hotfix branch.

The current `sprint-XX` receives the fix at the next sprint close. If someone needs it earlier, they merge `origin/develop` into their personal branch ([5.1](#51-bring-the-latest-sprint-changes-into-your-branch), with `origin/develop` instead of `origin/sprint-01`). If the hotfix includes a migration, it must be a new migration, never an edit of an existing one.

## 10. Releases and versioning

Releases are automated with [release-please](https://github.com/googleapis/release-please) (`.github/workflows/release-please.yml`). It runs **only** on pushes to `main` and always opens its PR against `main` (`target-branch: main`); it never runs on or targets `sprint-XX`, `develop`, or `release`:

- It reads the Conventional Commits merged into `main` since the last release, and opens or updates a PR `chore(repo): release vX.Y.Z` with the new version in `Directory.Build.props` (the line marked `x-release-please-version`) and `.release-please-manifest.json`, and the new `CHANGELOG.md` section.
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
- Runs `dotnet list package --vulnerable --include-transitive` before the sprint close and opens an issue for anything it reports.
- On Friday, runs the [sprint close checklist](#8-closing-a-sprint-friday) up to the release and the back-merge into `release`.
- Makes sure every Jira issue in the sprint is in the correct column and that closed issues meet the Definition of Done.

## 12. Definition of Done

An issue is **Done** only when all of the following are true:

- [ ] The acceptance criteria in Jira are met and were tested by the other team member.
- [ ] The code was reviewed and approved in a PR into `sprint-XX`, and the personal branch was deleted.
- [ ] CI is green (`branch-name`, `commit-messages`, and `ci`: format check, Release build with no warnings, tests, pending migrations check).
- [ ] It follows [docs/CODING_STANDARDS.md](docs/CODING_STANDARDS.md), with at least one test per acceptance criterion when feasible.
- [ ] Every new endpoint validates its input and is documented in OpenAPI (status codes and response types).
- [ ] Database changes are scripts in `database/` (never an edit of a merged migration script), and the migrator still runs twice without errors.
- [ ] Docs are updated (README, `.env.example`, `appsettings.json`) if applicable.
- [ ] The Jira issue is moved to **Done**.

## 13. Database workflow

The database is part of this repository and changes only through the scripts in `database/`, applied by `Itacr.Database.Migrator` (DbUp) on every machine and in the deploy workflow. **Never change the database by hand** (SSMS, the VS Code SQL Server extension, sqlcmd) except for reading data. The full reference is [database/README.md](database/README.md).

| What                                   | Where                                         | How it reaches the database                                  |
| -------------------------------------- | --------------------------------------------- | ------------------------------------------------------------ |
| Tables, columns, indexes, reference data | `database/migrations/V<NNN>__<description>.sql` | The migrator runs each script once, in order                 |
| Procedures, functions, views, triggers | `database/<type>/<Module>/<name>.sql`         | The migrator re-creates them on every run (`CREATE OR ALTER`) |
| Employees (test users, initial manager) | `database/Itacr.Database.Migrator/Seeding/`  | The migrator seeds them after the scripts (idempotent)       |
| Database users, reports, maintenance   | `database/scripts/`                           | Run by hand only                                             |

**Until ITACR-36 is merged, the schema is frozen:** the API still maps the tables with EF Core, so a table change would also need an EF Core migration for the CI check. ITACR-36 removes EF Core.

### 13.1 Change a table

```bash
# write database/migrations/V002__add_notes_to_orders.sql (header comment, additive change)
dotnet run --project database/Itacr.Database.Migrator   # applies it
dotnet run --project database/Itacr.Database.Migrator   # a second run must apply nothing
git add database/migrations/V002__add_notes_to_orders.sql
git commit -m "feat(db): add notes column to orders (ITACR-N)"
```

If the script is wrong and it is **not merged** into `sprint-XX` yet, fix the file and reset your local database ([13.3](#133-reset-your-local-database)). Once it is merged, never edit it: fix it with a new script.

### 13.2 Add or change a procedure, function, view, or trigger

```bash
# write or update database/functions/Orders/fn_GetOrdersByStatus.sql (header comment + one CREATE OR ALTER)
dotnet run --project database/Itacr.Database.Migrator
git add database/functions/Orders/fn_GetOrdersByStatus.sql
git commit -m "feat(db): add fn_GetOrdersByStatus function (ITACR-N)"
```

There is no migration to create: the file is applied on every run. Naming (`usp_`, `fn_`, `vw_`, `trg_`; never `sp_`), headers, `CREATE OR ALTER`, and the function-vs-procedure rule are in [section 3.2 of the coding standards](docs/CODING_STANDARDS.md#32-t-sql-objects-procedures-functions-views-and-triggers). Use only T-SQL that also runs on Azure SQL Database.

### 13.3 Reset your local database

```bash
docker compose down -v                                  # deletes the container's data
docker compose up -d sqlserver
dotnet run --project database/Itacr.Database.Migrator   # creates the database and applies every script and the seed
```

Only ever do this with your local Docker database.

## 14. Bootstrapping the backend (ITACR-26, ITACR-27, ITACR-28)

The repository starts with configuration only (SDK version, build settings, git hooks, CI, and docs). These three issues create the application and the database, **in this order**, each in its own personal branch from `sprint-01` and its own PR into `sprint-01`. Start each one only after the previous PR is merged.

### 14.1 ASP.NET Core API and database connection (ITACR-26)

```bash
git switch sprint-01
git pull
git switch -c feature/ITACR-26-api-base-and-db-connection
```

1. Create the solution, the API, and the test project:

   ```bash
   dotnet new sln -n Itacr
   dotnet new webapi --use-controllers -n Itacr.Api -o src/Itacr.Api --no-restore
   dotnet new xunit -n Itacr.Api.Tests -o tests/Itacr.Api.Tests --no-restore
   dotnet sln Itacr.slnx add src/Itacr.Api/Itacr.Api.csproj tests/Itacr.Api.Tests/Itacr.Api.Tests.csproj
   dotnet add tests/Itacr.Api.Tests/Itacr.Api.Tests.csproj reference src/Itacr.Api/Itacr.Api.csproj
   ```

   The .NET 10 SDK creates `Itacr.slnx`. If you get `Itacr.sln`, delete it and run `dotnet new sln -n Itacr --format slnx`.

2. Move the package versions to `Directory.Packages.props` (Central Package Management). In both `.csproj` files, for every `<PackageReference Include="X" Version="Y" />`:
   - add `<PackageVersion Include="X" Version="Y" />` inside the `<ItemGroup>` of `Directory.Packages.props`;
   - leave `<PackageReference Include="X" />` in the `.csproj`, without `Version`.

   Also delete `<Nullable>` and `<ImplicitUsings>` from both `.csproj` files: `Directory.Build.props` already sets them. Keep `<TargetFramework>net10.0</TargetFramework>`. Then run `dotnet restore`; error `NU1008` means a `Version` is still in a `.csproj`.

3. Add the packages and the EF Core tool (each `dotnet add package` writes its version to `Directory.Packages.props`):

   ```bash
   dotnet add src/Itacr.Api package Microsoft.EntityFrameworkCore.SqlServer
   dotnet add src/Itacr.Api package Microsoft.EntityFrameworkCore.Design
   dotnet add src/Itacr.Api package Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore
   dotnet add src/Itacr.Api package Scalar.AspNetCore
   dotnet add tests/Itacr.Api.Tests package Microsoft.AspNetCore.Mvc.Testing
   dotnet add tests/Itacr.Api.Tests package Microsoft.Extensions.TimeProvider.Testing
   dotnet new tool-manifest
   dotnet tool install dotnet-ef
   dotnet user-secrets init --project src/Itacr.Api
   ```

   Check in `Directory.Packages.props` and `.config/dotnet-tools.json` that every EF Core package and `dotnet-ef` share the same major version (10).

4. In `src/Itacr.Api/Properties/launchSettings.json`, keep only the `http` profile and set its `applicationUrl` to `http://localhost:3000` (the URL `itacr-frontend` uses). Delete the `WeatherForecast` sample files.

5. Add the local database: copy `compose.yaml` and `.env.example` (prepared for this issue) to the repository root, create your `.env`, and start SQL Server ([section 1](#1-one-time-setup)).

6. Configure the base app following [section 2 of the coding standards](docs/CODING_STANDARDS.md#2-aspnet-core):
   - `ItacrDbContext` in `Infrastructure/Persistence/`, registered with `UseAzureSql(connectionString, sql => sql.EnableRetryOnFailure())`. Read the connection string with `GetConnectionString("Default")` and fail at startup if it is missing.
   - `appsettings.json` with every section and safe values: `ConnectionStrings:Default` (empty), `Cors:Origins` (`["http://localhost:4200"]`), `RateLimiting`. Options classes validated with `ValidateDataAnnotations()` and `ValidateOnStart()`.
   - `AddProblemDetails()`, `UseExceptionHandler()`, and `UseStatusCodePages()` (RFC 9457 errors).
   - Controllers with `JsonStringEnumConverter` and lowercase URLs.
   - CORS from `Cors:Origins`, the built-in rate limiter, security headers, and HSTS plus HTTPS redirection outside Development.
   - `AddOpenApi()`, and in Development `MapOpenApi()` and `MapScalarApiReference()` (`/scalar/v1`).
   - `TimeProvider.System` registered as a singleton.
   - `AddHealthChecks().AddDbContextCheck<ItacrDbContext>()` and `MapHealthChecks("/api/v1/health")`.
   - JSON console logging outside Development.
   - An `Itacr.Api.http` file with the health request, and at least one test in `tests/Itacr.Api.Tests` (for example, with `WebApplicationFactory<Program>`, an unknown route returns `404` as `application/problem+json`).

7. Store your connection string ([section 1](#1-one-time-setup)), then verify:

   ```bash
   dotnet format
   dotnet build -c Release
   dotnet test
   docker compose up -d sqlserver
   dotnet run --project src/Itacr.Api
   ```

   `GET http://localhost:3000/api/v1/health` must return `200` (`Healthy`) with the database running, and `503` with it stopped.

8. Commit and open the PR into `sprint-01`:

   ```bash
   git add Itacr.slnx src tests Directory.Packages.props .config compose.yaml .env.example
   git status                       # check that no bin/, obj/, .env, or secret is staged
   git commit -m "feat(core): scaffold asp.net core api with database connection (ITACR-26)"
   git push -u origin feature/ITACR-26-api-base-and-db-connection
   ```

From this PR on, CI detects `Itacr.slnx` and runs the format check, the Release build, and the tests.

### 14.2 Database schema (ITACR-27)

Start from `sprint-01` after ITACR-26 is merged:

```bash
git switch sprint-01
git pull
git switch -c feature/ITACR-27-database-schema
```

1. Add the `database/` folder (prepared for this issue: `README.md` and the `procedures/`, `functions/`, `views/`, `triggers/`, and `scripts/` folders).
2. Model the tables of the entity inventory (section 7.6 of the project document) following [section 3.1 of the coding standards](docs/CODING_STANDARDS.md#31-tables-with-ef-core): entities in `Domain/`, one `IEntityTypeConfiguration<T>` per entity in `Infrastructure/Persistence/Configurations/`, a `DbSet` per entity in `ItacrDbContext`, and `ApplyConfigurationsFromAssembly` in `OnModelCreating`. Include `CreatedAt`/`UpdatedAt` (set by a `SaveChanges` interceptor), indexes, a maximum length on every string, `HasPrecision(10, 2)` for money, enums as text, and a JSON column (`ToJson()`) for combo customizations.
3. Create the first migration in the agreed folder and apply it:

   ```bash
   dotnet ef migrations add InitialSchema --project src/Itacr.Api --output-dir Infrastructure/Persistence/Migrations
   dotnet ef database update --project src/Itacr.Api
   ```

   Later migrations go to the same folder automatically.

4. Add any procedure, function, view, or trigger with the workflow in [section 13.2](#132-add-or-change-a-procedure-function-view-or-trigger).
5. Commit with the `db` scope and open the PR into `sprint-01`:

   ```bash
   git add src/Itacr.Api database
   git commit -m "feat(db): add initial database schema (ITACR-27)"
   git push -u origin feature/ITACR-27-database-schema
   ```

From this PR on, CI also checks that the model has no changes without a migration.

### 14.3 Seed data (ITACR-28)

Branch `feature/ITACR-28-seed-data` from `sprint-01` after ITACR-27 is merged.

1. Write the seeder in `Infrastructure/Persistence/Seeding/` with categories, products, tables, and test users. Before inserting each row, check whether it already exists (by a natural key such as the category name or the user's employee number), so it can run any number of times. Hash every seeded PIN or password with `PasswordHasher<T>`.
2. Register it in the `DbContext` options with both `UseSeeding` and `UseAsyncSeeding` (EF Core calls the synchronous one from `dotnet ef database update` and the asynchronous one from `MigrateAsync`).
3. Verify idempotency by running it twice on a fresh database:

   ```bash
   dotnet ef database drop --force --project src/Itacr.Api
   dotnet ef database update --project src/Itacr.Api
   dotnet ef database update --project src/Itacr.Api
   ```

4. Commit `feat(db): add idempotent seed data (ITACR-28)` and open the PR into `sprint-01`.

## 15. Deployment to Azure

The Azure resources are created by hand in the Azure Portal (see [README: Deployment](README.md#deployment)). This repository never creates or changes them.

### 15.1 App Service settings

The production settings (`ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__Default`, `Jwt__Secret`, `Cors__Origins__0`) are listed in [README: Production settings](README.md#production-settings). They are set in App Service `app-itacr-backend-prod` > Settings > **Environment variables**, never in the repository, and must exist before the first deploy: the API validates them at startup and does not start without them.

### 15.2 Deploy workflow

`.github/workflows/deploy.yml` runs on every push to `main` (and by hand with **Run workflow**, from `main`). It has three jobs; each starts only if the previous one succeeded:

| Job | What it does |
| --- | ------------ |
| `build` | Restores, builds, and tests the solution, and publishes the API and `Itacr.Database.Migrator` as artifacts (kept 3 days) |
| `migrate` | Signs in to Azure with OIDC, opens a firewall rule on `itacr-sql` for the runner's IP, runs the migrator as `itacr_migrator`, and removes the rule even if the migrator failed |
| `deploy` | Deploys the API to `app-itacr-backend-prod` with the `AZURE_WEBAPP_PUBLISH_PROFILE` secret |

The database is migrated **before** the new API starts, so the scripts must be additive: for a moment the previous API runs against the new schema. If the migrator fails, its script is rolled back, the job fails, and the API is not deployed. Rename or drop columns in a later release.

The seed runs in `Production`, so it only creates the initial manager while no manager exists, from `Seed__ManagerName`, `Seed__ManagerEmail`, and `Seed__ManagerPin`. Production already has its manager, so those variables are not set in the workflow.

**One-time setup (repository owner):** App Service `app-itacr-backend-prod` > Overview > **Download publish profile**, then paste the whole file into the repository secret `AZURE_WEBAPP_PUBLISH_PROFILE` (Settings > Secrets and variables > Actions). Delete the downloaded file afterwards, and never commit it. Downloading it requires App Service > Configuration > **SCM Basic Auth Publishing Credentials** turned on. If the file is ever exposed, reset it (Overview > **Reset publish profile**) and update the secret.

### 15.3 Database access from the workflow

The `migrate` job needs two things, both already set up:

1. **An Azure identity without stored passwords (OIDC).** App registration `github-itacr-backend-deploy` with a federated credential for the subject `repo:JulianH2/itacr-backend:ref:refs/heads/main`, so only runs on `main` can sign in. Its service principal has a single role: **SQL Server Contributor** on the server `itacr-sql`, which lets it add and remove firewall rules. The workflow uses no GitHub environment; adding `environment:` to the job would change the subject and need a new federated credential.
2. **Its own database user.** `itacr_migrator`, created with [database/scripts/security/database-users.sql](database/scripts/security/database-users.sql).

| Repository secret | Value |
| ----------------- | ----- |
| `AZURE_CLIENT_ID` | Application (client) ID of `github-itacr-backend-deploy` |
| `AZURE_TENANT_ID` | Directory (tenant) ID |
| `AZURE_SUBSCRIPTION_ID` | Subscription ID of Azure for Students |
| `SQL_MIGRATOR_CONNECTION_STRING` | `Server=tcp:itacr-sql.database.windows.net,1433;Initial Catalog=itacr-sql;User ID=itacr_migrator;Password=<password>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60` |

**Database users (repository owner, once).** No application uses the server admin login:

1. In the Azure Portal, open the `itacr-sql` **database** > **Query editor**, sign in as the server admin, and run `database/scripts/security/database-users.sql` after replacing its two password placeholders with generated passwords. Keep them only in a password manager.
2. Save the `itacr_migrator` connection string in the secret `SQL_MIGRATOR_CONNECTION_STRING`.
3. Change `ConnectionStrings__Default` in App Service `app-itacr-backend-prod` to the same connection string with `User ID=itacr_app` and its password. Save, wait for the restart, and check `GET /api/v1/health` returns `200 Healthy`. The health check only opens a connection (it proves the login and the firewall, not the permissions), so also check that `itacr_app` can read and write, in this order:

   | Check | Request | Expected | Proves |
   | ----- | ------- | -------- | ------ |
   | Read a table | `GET /api/v1/catalog/categories` | `200`, 5 categories | `SELECT` on `dbo.Categories` |
   | Read through the function | `GET /api/v1/catalog/products?categoryId=10000000-0000-0000-0000-000000000004` | `200`, 4 drinks | `SELECT` on `dbo.fn_GetProductsByCategory` |
   | Write | `POST /api/v1/auth/login/pin` with the manager's `employeeId` (from `GET /api/v1/auth/employees?role=Manager`) and a **wrong** PIN, **once** | `401`, not `500` | `UPDATE` of `FailedLoginAttempts` (a missing permission fails `SaveChanges` with `500`) |
   | Write and clean up | The same request with the **correct** PIN | `200` with a token | `UPDATE` that resets `FailedLoginAttempts` to `0` |

   Send the wrong PIN only once: after 5 failed attempts (`LoginSecurity:MaxFailedPinAttempts`) the account is locked for 15 minutes (`LoginSecurity:LockoutMinutes`). The correct PIN alone does not prove the write: when the counter is already `0`, EF Core sends no `UPDATE`.

4. Only then reset the server admin password (SQL server `itacr-sql` > Overview > **Reset password**) and keep the new one only in the password manager, for emergencies.

### 15.4 Run the migrator against Azure SQL by hand

Only if the workflow cannot run it. From your machine, in PowerShell:

1. Allow your IP: SQL server `itacr-sql` > Security > **Networking** > **Add your client IPv4 address** > Save.
2. Run the migrator with the `itacr_migrator` connection string:

   ```powershell
   $env:DOTNET_ENVIRONMENT = 'Production'
   $env:ConnectionStrings__Migrator = '<the SQL_MIGRATOR_CONNECTION_STRING value>'
   dotnet run --project database/Itacr.Database.Migrator --no-launch-profile
   Remove-Item Env:ConnectionStrings__Migrator, Env:DOTNET_ENVIRONMENT
   ```

   `--no-launch-profile` keeps `launchSettings.json` from switching the environment to `Development`.
3. Delete your IP rule in the **Networking** page.
