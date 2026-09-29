# database/

SQL objects that Prisma does not model. Tables, enums, relations, and indexes live in `prisma/schema.prisma`; everything here reaches the database **only through a Prisma migration**. Full rules: [docs/CODING_STANDARDS.md, section 4.2](../docs/CODING_STANDARDS.md#42-sql-objects-procedures-functions-views-and-triggers).

| Folder        | Contents                                                              | Prefix |
| ------------- | --------------------------------------------------------------------- | ------ |
| `procedures/` | Stored procedures (`CALL`), for operations that change data           | `sp_`  |
| `functions/`  | Functions, including queries that return rows (`RETURNS TABLE`)       | `fn_`  |
| `views/`      | Views                                                                 | `vw_`  |
| `triggers/`   | Triggers (and their trigger function in the same file)                | `trg_` |
| `scripts/`    | One-off maintenance or reporting scripts, never applied automatically | -      |

## Rules

- One object per file, named exactly like the object: `functions/fn_get_orders_by_status.sql`.
- snake_case names; parameters start with `p_`, local variables with `v_`.
- Use `CREATE OR REPLACE` so the file can be re-run.
- Every file starts with this header:

```sql
-- =============================================================================
-- Object:      <object name>
-- Type:        procedure | function | view | trigger | script
-- Purpose:     <what it does and why it exists>
-- Parameters:  <p_name type - description>, or "none"
-- Returns:     <return type>, or "none"
-- Author:      <GitHub username>
-- Jira:        ITACR-<N>
-- =============================================================================
```

## Applying an object to the database

```bash
# 1. Write or update the file, for example database/functions/fn_get_orders_by_status.sql
# 2. Create an empty migration
npx prisma migrate dev --create-only --name add_fn_get_orders_by_status
# 3. Copy the full SQL file into prisma/migrations/<timestamp>_add_fn_get_orders_by_status/migration.sql
# 4. Apply it
npx prisma migrate dev
# 5. Commit the SQL file and the migration together
```

To change an object later, edit its file here and create a **new** migration with the full new definition. Never edit a migration that was already applied.
