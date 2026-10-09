using Dapper;
using DbUp;
using DbUp.Engine;
using DbUp.ScriptProviders;
using DbUp.Support;
using Itacr.Database.Migrator.Seeding;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Itacr.Database.Migrator;

/// <summary>
/// Brings a database to the current version: versioned scripts in <c>migrations/</c> run once, in order;
/// the T-SQL objects in <c>functions/</c>, <c>views/</c>, <c>procedures/</c>, and <c>triggers/</c> are
/// re-created on every run; then the employees are seeded.
/// </summary>
public sealed class DatabaseMigrator(
    IConfiguration configuration,
    IHostEnvironment environment,
    EmployeeSeeder employeeSeeder,
    ILogger<DatabaseMigrator> logger)
{
    public const string ConnectionStringName = "Migrator";

    private const string InitialSchemaScript = "V001__initial_schema.sql";

    // The last EF Core migration applied before ITACR-37; databases created by EF Core are baselined at V001.
    private const string LastEfCoreMigration = "20261002130000_AddFnGetProductsByCategory";

    private const int MaxConnectionAttempts = 6;
    private static readonly TimeSpan ConnectionRetryDelay = TimeSpan.FromSeconds(10);

    private static readonly string ScriptsRoot = Path.Combine(AppContext.BaseDirectory, "Scripts");

    private static readonly (string Folder, ScriptType Type)[] ScriptGroups =
    [
        ("migrations", ScriptType.RunOnce),
        ("functions", ScriptType.RunAlways),
        ("views", ScriptType.RunAlways),
        ("procedures", ScriptType.RunAlways),
        ("triggers", ScriptType.RunAlways),
    ];

    public async Task<bool> RunAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogError(
                "The connection string 'ConnectionStrings:{Name}' is missing. Set it with dotnet user-secrets or the ConnectionStrings__{Name} environment variable",
                ConnectionStringName,
                ConnectionStringName);
            return false;
        }

        if (environment.IsDevelopment())
        {
            EnsureDatabase.For.SqlDatabase(connectionString);
        }

        await WaitForDatabaseAsync(connectionString, cancellationToken);

        var upgrader = BuildUpgrader(connectionString);
        var efCoreState = await GetEfCoreStateAsync(connectionString, upgrader, cancellationToken);
        if (efCoreState == EfCoreState.Incomplete)
        {
            logger.LogError(
                "The database has EF Core migrations but not {LastMigration}. Apply the EF Core migrations of release v0.1.x first",
                LastEfCoreMigration);
            return false;
        }

        if (efCoreState == EfCoreState.NeedsBaseline)
        {
            var baseline = upgrader.MarkAsExecuted(InitialSchemaScript);
            if (!baseline.Successful)
            {
                logger.LogError(baseline.Error, "Could not mark {Script} as executed", InitialSchemaScript);
                return false;
            }

            logger.LogInformation("Database created by EF Core: {Script} marked as executed without running it", InitialSchemaScript);
        }

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            logger.LogError(result.Error, "Script {Script} failed; the database was left at the last successful script", result.ErrorScript?.Name);
            return false;
        }

        await employeeSeeder.SeedAsync(connectionString, environment.IsDevelopment(), cancellationToken);
        logger.LogInformation("Database is up to date");
        return true;
    }

    private static UpgradeEngine BuildUpgrader(string connectionString)
    {
        var builder = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithTransactionPerScript()
            .WithVariablesDisabled()
            .LogToConsole();

        for (var index = 0; index < ScriptGroups.Length; index++)
        {
            var (folder, type) = ScriptGroups[index];
            var path = Path.Combine(ScriptsRoot, folder);
            if (!Directory.Exists(path))
            {
                continue;
            }

            builder.WithScripts(new FileSystemScriptProvider(
                path,
                new FileSystemScriptOptions { IncludeSubDirectories = true, UseOnlyFilenameForScriptName = type == ScriptType.RunOnce },
                new SqlScriptOptions { ScriptType = type, RunGroupOrder = index + 1 }));
        }

        return builder.Build();
    }

    private async Task WaitForDatabaseAsync(string connectionString, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                return;
            }
            catch (SqlException exception) when (attempt < MaxConnectionAttempts)
            {
                // Azure SQL serverless takes up to a minute to resume after an auto-pause.
                logger.LogWarning(
                    "Database not reachable (attempt {Attempt} of {MaxAttempts}, SQL error {Number}); retrying in {Delay}",
                    attempt,
                    MaxConnectionAttempts,
                    exception.Number,
                    ConnectionRetryDelay);
                await Task.Delay(ConnectionRetryDelay, cancellationToken);
            }
        }
    }

    private static async Task<EfCoreState> GetEfCoreStateAsync(
        string connectionString,
        UpgradeEngine upgrader,
        CancellationToken cancellationToken)
    {
        if (upgrader.GetExecutedScripts().Contains(InitialSchemaScript))
        {
            return EfCoreState.NotApplicable;
        }

        await using var connection = new SqlConnection(connectionString);
        var historyExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT CAST(CASE WHEN OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL THEN 0 ELSE 1 END AS bit);",
            cancellationToken: cancellationToken));
        if (!historyExists)
        {
            return EfCoreState.NotApplicable;
        }

        // A separate statement: SQL Server compiles a whole statement, so it cannot reference the table before it is known to exist.
        var lastMigrationApplied = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = @LastMigration) THEN 1 ELSE 0 END AS bit);",
            new { LastMigration = LastEfCoreMigration },
            cancellationToken: cancellationToken));
        return lastMigrationApplied ? EfCoreState.NeedsBaseline : EfCoreState.Incomplete;
    }

    private enum EfCoreState
    {
        NotApplicable,
        NeedsBaseline,
        Incomplete,
    }
}
