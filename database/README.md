# database/

T-SQL objects that EF Core does not model, one object per file. Tables, columns, relations, and indexes are **not** here: they live in the EF Core model (`src/Itacr.Domain/` and `src/Itacr.Infrastructure/Persistence/`) and reach the database through migrations.

The same scripts run on the local SQL Server container and on Azure SQL Database (production), so use only features available in both.

```
database/
├── procedures/   # usp_*.sql  Operations that change data or need transaction control (EXEC dbo.usp_...)
├── functions/    # fn_*.sql   Inline table-valued functions for queries (SELECT ... FROM dbo.fn_...(...))
├── views/        # vw_*.sql   Read-only views for reports and the manager dashboard
├── triggers/     # trg_*.sql  Triggers
└── scripts/      # One-off reports or local maintenance; never applied automatically
```

## Rules

- The file name is exactly the object name: `database/functions/fn_GetOrdersByStatus.sql`.
- Every file starts with the header comment (object, type, purpose, parameters, returns, author, Jira key) followed by a single `CREATE OR ALTER` statement on the `dbo` schema.
- No `GO` separators and no `USE` statements: each file is exactly one batch.
- The file always holds the **current** version of the object. The migrations hold the history.
- **A file in this folder does nothing on its own.** It reaches the database only through an EF Core migration that runs its SQL with `migrationBuilder.Sql(...)`.
- Never change the database by hand (SSMS, the VS Code SQL Server extension, sqlcmd) except to read data.

## Add or change an object

1. Write or update the file here.
2. Create an empty migration (no model changes):

   ```bash
   dotnet ef migrations add AddFnGetOrdersByStatus --project src/Itacr.Infrastructure --startup-project src/Itacr.Api
   ```

3. Paste the full content of the file into the migration's `Up`, and drop the object in `Down`:

   ```csharp
   protected override void Up(MigrationBuilder migrationBuilder)
   {
       migrationBuilder.Sql("""
           -- full content of database/functions/fn_GetOrdersByStatus.sql
           """);
   }

   protected override void Down(MigrationBuilder migrationBuilder)
   {
       migrationBuilder.Sql("DROP FUNCTION IF EXISTS dbo.fn_GetOrdersByStatus;");
   }
   ```

4. Apply it with `dotnet ef database update --project src/Itacr.Infrastructure --startup-project src/Itacr.Api`, and commit the file and the migration together.

Full naming and style rules: [docs/CODING_STANDARDS.md, section 3.2](../docs/CODING_STANDARDS.md#32-t-sql-objects-procedures-functions-views-and-triggers).
