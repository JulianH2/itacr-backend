# database/

Everything that reaches the database lives here, and it reaches the database only through `Itacr.Database.Migrator`, a console app built on [DbUp](https://dbup.readthedocs.io/). The same scripts run on the local SQL Server 2025 container and on Azure SQL Database (production), so use only T-SQL available in both.

```
database/
├── migrations/                 # V001__initial_schema.sql, V002__...: schema and reference data; each runs once, in order
├── functions/<Module>/         # fn_*.sql   Inline table-valued functions       ┐
├── views/<Module>/             # vw_*.sql   Views                               │ CREATE OR ALTER, applied again
├── procedures/<Module>/        # usp_*.sql  Stored procedures                   │ on every run, in this order
├── triggers/<Module>/          # trg_*.sql  Triggers                            ┘
├── scripts/                    # Run by hand only; never applied by the migrator
│   └── security/               # database-users.sql, app-execute-only.sql
└── Itacr.Database.Migrator/    # DbUp console app, plus the employee seed
```

`<Module>` uses the same module names as the code (`Identity`, `Catalog`, `Orders`, `Kitchen`, ...).

## What the migrator does

Every run, in this order:

1. **Development only:** creates the database if it does not exist.
2. Waits for the database (up to 6 attempts, 10 seconds apart): Azure SQL serverless takes up to a minute to resume after an auto-pause.
3. **EF Core baseline:** if the database was created by the EF Core migrations of sprint 1 (`dbo.__EFMigrationsHistory` contains `20261002130000_AddFnGetProductsByCategory`) and `V001` is not in the journal yet, it marks `V001` as executed without running it. Production and any local database created before ITACR-37 are in this state. If the EF Core history exists but is incomplete, the migrator stops with an error.
4. Runs every script in `migrations/` that is not in the journal table `dbo.SchemaVersions`, in name order, each in its own transaction. A failed script rolls back and stops the run.
5. Runs every script in `functions/`, `views/`, `procedures/`, and `triggers/`. They are `CREATE OR ALTER`, so running them again is safe, and the file is always the current version of the object.
6. Seeds employees ([below](#seed-data)).

It exits with code `0` on success and `1` on any error, so the deploy stops before the new API is published.

| Environment | Connection string | Environment name |
| ----------- | ----------------- | ---------------- |
| Local | `dotnet user-secrets` of the migrator: `ConnectionStrings:Migrator` | `Development` (from `Properties/launchSettings.json`) |
| QA (database `itacr-qa`, deployed from `release`) | `ConnectionStrings__Migrator` from the GitHub secret `SQL_MIGRATOR_QA_CONNECTION_STRING` (user `itacr_migrator` of `itacr-qa`) | `Production`, with `Seed__TestEmployees=true` |
| Production | `ConnectionStrings__Migrator` from the GitHub secret `SQL_MIGRATOR_CONNECTION_STRING` (user `itacr_migrator`) | `Production` |

## Run it locally

```bash
docker compose up -d sqlserver
dotnet user-secrets set "ConnectionStrings:Migrator" "Server=localhost,1433;Database=Itacr;User Id=sa;Password=<MSSQL_SA_PASSWORD from .env>;TrustServerCertificate=True" --project database/Itacr.Database.Migrator
dotnet run --project database/Itacr.Database.Migrator
```

Run it again after every pull that brings new scripts. To start from an empty database: `docker compose down -v`, `docker compose up -d sqlserver`, and run the migrator.

## Add a schema change (table, column, index, reference data)

1. Create the next script in `migrations/`: `V002__add_order_status_history.sql`. The number is the next free one, with three digits; the description is lowercase snake case.
2. Start it with the header comment (migration, purpose, author, Jira key). The script can have several batches separated by `GO`.
3. Keep it additive (new tables, nullable or defaulted columns): during a deploy the previous API runs for a moment against the new schema. Rename or drop columns in a later release.
4. Run the migrator locally, twice: the second run must apply nothing.
5. Commit the script with the `db` scope: `feat(db): add order status history table (ITACR-N)`.

**Never edit or rename a script that is already in `sprint-XX`, `develop`, `release`, or `main`**: the journal stores its name and it will not run again. Fix it with a new script. If you and your teammate both create `V002`, whoever merges second renames theirs to `V003` before merging.

## Add or change a function, view, procedure, or trigger

1. Write or update the file in its folder, under the module: `procedures/Orders/usp_Order_Create.sql`.
2. The file name is exactly the object name. It starts with the header comment (object, type, purpose, parameters, returns, author, Jira key) followed by a single `CREATE OR ALTER` statement on the `dbo` schema. No `GO` and no `USE`.
3. Run the migrator locally and commit the file. There is no migration to create: the file itself is applied on every run.
4. To delete an object, remove its file and add a migration with `DROP ... IF EXISTS`.

Objects are applied in folder order (functions, views, procedures, triggers), and in name order inside a folder. An object that uses another object of the same folder must sort after it.

## Seed data

| What | Where | Environments |
| ---- | ----- | ------------ |
| Catalog: 5 categories, 18 products, 9 combo options, dining tables 1 to 10, with fixed IDs | `migrations/V001__initial_schema.sql` | All |
| 4 test employees (password `Itacr2026!`) | `Itacr.Database.Migrator/Seeding/` | Development, and QA through `Seed__TestEmployees=true`. Never production: their credentials are in this repository |
| Initial manager from `Seed__ManagerName`, `Seed__ManagerEmail`, `Seed__ManagerPin` | `Itacr.Database.Migrator/Seeding/` | Any other environment, only while no manager exists |

Employees are seeded in C# because their PINs and passwords are hashed with the same `PasswordHasher` the API verifies with. The seed checks existing emails first, so it never creates duplicates.

## Database users

The migrator and the API use different contained users, created once by hand with [scripts/security/database-users.sql](scripts/security/database-users.sql) (placeholders only; the real passwords live in a password manager, the GitHub secret, and the App Service settings):

| User | Permissions | Used by |
| ---- | ----------- | ------- |
| `itacr_migrator` | `db_ddladmin`, `db_datareader`, `db_datawriter` | The migrator in the deploy workflow |
| `itacr_app` | `EXECUTE` on `dbo` only. Until the release with ITACR-36 is deployed it also has `db_datareader` and `db_datawriter`, removed afterwards with [scripts/security/app-execute-only.sql](scripts/security/app-execute-only.sql) | The API (`ConnectionStrings__Default`) |

The server admin login is not used by any application.

Full naming and style rules: [docs/CODING_STANDARDS.md, section 3](../docs/CODING_STANDARDS.md#3-database-azure-sql--dbup).
