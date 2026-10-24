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

/// <summary>How long the migrator waits for SQL Server: a container that is still starting, or a paused serverless database.</summary>
public sealed record ConnectionRetryPolicy(int MaxAttempts, TimeSpan Delay)
{
    public static readonly ConnectionRetryPolicy Default = new(6, TimeSpan.FromSeconds(10));
}

/// <summary>
/// Brings a database to the current version: versioned scripts in <c>migrations/</c> run once, in order;
/// the T-SQL objects in <c>functions/</c>, <c>views/</c>, <c>procedures/</c>, and <c>triggers/</c> are
/// re-created on every run; then the employees are seeded. It never throws: any failure is logged as one
/// clear message and <see cref="RunAsync"/> returns false, so the process exits with code 1.
/// </summary>
public sealed class DatabaseMigrator(
    IConfiguration configuration,
    IHostEnvironment environment,
    EmployeeSeeder employeeSeeder,
    ILogger<DatabaseMigrator> logger,
    ConnectionRetryPolicy? retryPolicy = null)
{
    public const string ConnectionStringName = "Migrator";

    private const string InitialSchemaScript = "V001__initial_schema.sql";

    // The last EF Core migration applied before ITACR-37; databases created by EF Core are baselined at V001.
    private const string LastEfCoreMigration = "20261002130000_AddFnGetProductsByCategory";

    private static readonly string ScriptsRoot = Path.Combine(AppContext.BaseDirectory, "Scripts");

    private readonly ConnectionRetryPolicy _retryPolicy = retryPolicy ?? ConnectionRetryPolicy.Default;

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
        try
        {
            return await MigrateAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogError("The migration was cancelled; the database was left at the last successful script");
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError("The migration stopped: {ExceptionType}: {Message}", exception.GetType().Name, exception.Message);
            logger.LogDebug(exception, "Migration failure details");
            return false;
        }
    }

    /// <summary>Logs the versioned scripts already applied (with their date) and the ones still pending. Changes nothing.</summary>
    public Task<bool> ShowStatusAsync(CancellationToken cancellationToken) => GuardAsync(async () =>
    {
        if (ConnectionString() is not { } connectionString)
        {
            return false;
        }

        var upgrader = BuildUpgrader(connectionString);
        await using var connection = new SqlConnection(connectionString);
        var applied = await connection.QueryAsync<(string ScriptName, DateTime Applied)>(new CommandDefinition(
            "IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NOT NULL SELECT ScriptName, Applied FROM dbo.SchemaVersions ORDER BY Id;",
            cancellationToken: cancellationToken));
        foreach (var script in applied)
        {
            logger.LogInformation("Applied  {Script} on {Applied:yyyy-MM-dd HH:mm:ss}", script.ScriptName, script.Applied);
        }

        var pending = upgrader.GetScriptsToExecute().Where(script => script.SqlScriptOptions.ScriptType == ScriptType.RunOnce).ToList();
        foreach (var script in pending)
        {
            logger.LogInformation("Pending  {Script}", script.Name);
        }

        logger.LogInformation("{Applied} versioned scripts applied, {Pending} pending", applied.Count(), pending.Count);
        return true;
    }, cancellationToken);

    /// <summary>
    /// Reverses the last applied versioned script with its script in <c>rollbacks/</c> (same file name) and removes it from
    /// dbo.SchemaVersions, in one transaction, so the next run applies it again. Development only: QA and production never
    /// go back.
    /// </summary>
    public Task<bool> RollbackAsync(string scriptName, CancellationToken cancellationToken) => GuardAsync(async () =>
    {
        if (!environment.IsDevelopment())
        {
            logger.LogError("Rollback only runs in Development; the {Environment} database was not changed", environment.EnvironmentName);
            return false;
        }

        if (ConnectionString() is not { } connectionString)
        {
            return false;
        }

        var rollbackPath = Path.Combine(ScriptsRoot, "rollbacks", Path.GetFileName(scriptName));
        if (!File.Exists(rollbackPath))
        {
            logger.LogError("There is no rollback script for {Script} in database/rollbacks", scriptName);
            return false;
        }

        var lastApplied = BuildUpgrader(connectionString).GetExecutedScripts().LastOrDefault();
        if (!string.Equals(lastApplied, scriptName, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogError("Only the last applied script can be reversed: {LastApplied}, not {Script}", lastApplied, scriptName);
            return false;
        }

        var script = await File.ReadAllTextAsync(rollbackPath, cancellationToken);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var batch in script.Replace("\r\n", "\n").Split("\nGO\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            await connection.ExecuteAsync(new CommandDefinition(batch, transaction: transaction, cancellationToken: cancellationToken));
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM dbo.SchemaVersions WHERE ScriptName = @ScriptName;",
            new { ScriptName = lastApplied },
            transaction,
            cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("{Script} reversed and removed from dbo.SchemaVersions; the next run applies it again", lastApplied);
        return true;
    }, cancellationToken);

    private async Task<bool> GuardAsync(Func<Task<bool>> action, CancellationToken cancellationToken)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogError("The command was cancelled");
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError("The command stopped: {ExceptionType}: {Message}", exception.GetType().Name, exception.Message);
            logger.LogDebug(exception, "Failure details");
            return false;
        }
    }

    private string? ConnectionString()
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogError(
                "The connection string 'ConnectionStrings:{Name}' is missing. Set it with dotnet user-secrets or the ConnectionStrings__{Name} environment variable",
                ConnectionStringName,
                ConnectionStringName);
            return null;
        }

        return connectionString;
    }

    private async Task<bool> MigrateAsync(CancellationToken cancellationToken)
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
            // Creating the database connects to master, so it is retried like any other connection.
            await RetryWhileUnreachableAsync(
                "SQL Server",
                () =>
                {
                    EnsureDatabase.For.SqlDatabase(connectionString);
                    return Task.CompletedTask;
                },
                cancellationToken);
        }

        await RetryWhileUnreachableAsync(
            "Database",
            async () =>
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
            },
            cancellationToken);

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

    private async Task RetryWhileUnreachableAsync(string target, Func<Task> connect, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await connect();
                return;
            }
            catch (SqlException exception) when (attempt < _retryPolicy.MaxAttempts)
            {
                // A SQL Server container still starting resets connections (10054) or rejects logins; Azure SQL
                // serverless takes up to a minute to resume after an auto-pause.
                logger.LogWarning(
                    "{Target} not reachable (attempt {Attempt} of {MaxAttempts}, SQL error {Number}: {Message}); retrying in {Delay}",
                    target,
                    attempt,
                    _retryPolicy.MaxAttempts,
                    exception.Number,
                    exception.Message,
                    _retryPolicy.Delay);
                await Task.Delay(_retryPolicy.Delay, cancellationToken);
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
