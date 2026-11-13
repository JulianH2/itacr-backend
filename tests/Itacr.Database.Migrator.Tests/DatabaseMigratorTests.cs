using Dapper;
using Itacr.Database.Migrator.Seeding;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Itacr.Database.Migrator.Tests;

[Collection(SqlServerCollection.Name)]
public sealed partial class DatabaseMigratorTests(SqlServerFixture sqlServer)
{
    private const string InitialSchemaScript = "V001__initial_schema.sql";
    private const string OrderStatusHistoryScript = "V002__add_order_status_history.sql";
    private const string PickupConstraintsScript = "V003__add_pickup_order_constraints.sql";
    private const string WebpImagesScript = "V004__use_webp_product_images.sql";
    private const string ProductAvailabilityScript = "V005__add_product_is_available.sql";
    private const string PushEndpointIndexScript = "V006__add_push_subscription_endpoint_index.sql";
    private const string ExclusionOptionsScript = "V007__add_combo_exclusion_options.sql";
    private const string PrivacyNoticeScript = "V008__add_employee_privacy_notice_date.sql";
    private const string OrderModifiedAtScript = "V009__add_order_modified_at.sql";
    private const string ResolvedAtScript = "V010__add_notification_resolved_at.sql";
    private const string PushEndpointIndexQuery = "SELECT COUNT(*) FROM sys.indexes WHERE name = N'UX_PushSubscriptions_Endpoint';";
    private const string LastEfCoreMigration = "20261002130000_AddFnGetProductsByCategory";

    private static readonly PasswordHasher<Employee> PasswordHasher = new();
    private static readonly string[] AllScripts = [InitialSchemaScript, OrderStatusHistoryScript, PickupConstraintsScript, WebpImagesScript, ProductAvailabilityScript, PushEndpointIndexScript, ExclusionOptionsScript, PrivacyNoticeScript, OrderModifiedAtScript, ResolvedAtScript];

    [Fact]
    public async Task RunAsync_ShouldCreateTheSchemaAndSeedTheTestEmployees_WhenDatabaseDoesNotExist()
    {
        // Arrange
        var connectionString = sqlServer.ConnectionStringFor(NewDatabaseName());

        // Act
        var succeeded = await CreateMigrator(connectionString, Environments.Development).RunAsync(CancellationToken.None);

        // Assert
        Assert.True(succeeded);
        await using var connection = new SqlConnection(connectionString);
        Assert.Equal(5, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Categories;"));
        Assert.Equal(18, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Products;"));
        Assert.Equal(4, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.fn_GetProductsByCategory('10000000-0000-0000-0000-000000000004');"));
        Assert.Equal(AllScripts, await ExecutedScriptsAsync(connection));
        Assert.Equal("Brownie con Helado", await connection.ExecuteScalarAsync<string>(
            "SELECT Name FROM dbo.Products WHERE Id = '20000000-0000-0000-0000-000000000018' AND CategoryId = '10000000-0000-0000-0000-000000000005';"));
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sys.identity_columns AS c JOIN sys.tables AS t ON t.object_id = c.object_id WHERE t.is_ms_shipped = 0 AND t.name <> N'SchemaVersions';"));

        var waiter = await connection.QuerySingleAsync<(string Role, string Status, string PinHash, string PasswordHash)>(
            "SELECT Role, Status, PinHash, PasswordHash FROM dbo.Employees WHERE Email = 'mesero@itacr.test';");
        Assert.Equal((nameof(EmployeeRole.Waiter), nameof(EmployeeStatus.Active)), (waiter.Role, waiter.Status));
        Assert.Equal(PasswordVerificationResult.Success, VerifyHash(waiter.PinHash, "1111"));
        Assert.Equal(PasswordVerificationResult.Success, VerifyHash(waiter.PasswordHash, "Itacr2026!"));
        Assert.Equal(4, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Employees;"));
    }

    [Fact]
    public async Task RunAsync_ShouldChangeNothing_WhenItRunsTwice()
    {
        // Arrange
        var connectionString = sqlServer.ConnectionStringFor(NewDatabaseName());
        var migrator = CreateMigrator(connectionString, Environments.Development);
        Assert.True(await migrator.RunAsync(CancellationToken.None));

        // Act
        var succeeded = await migrator.RunAsync(CancellationToken.None);

        // Assert
        Assert.True(succeeded);
        await using var connection = new SqlConnection(connectionString);
        Assert.Equal(4, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Employees;"));
        Assert.Equal(18, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Products;"));
        Assert.Equal(AllScripts, await ExecutedScriptsAsync(connection));
    }

    [Fact]
    public async Task RunAsync_ShouldMarkTheInitialSchemaAsExecutedWithoutRunningIt_WhenDatabaseWasCreatedByEfCore()
    {
        // Arrange
        var connectionString = await sqlServer.CreateDatabaseAsync(NewDatabaseName());
        await CreateEfCoreDatabaseAsync(connectionString, LastEfCoreMigration);

        // Act
        var succeeded = await CreateMigrator(connectionString, Environments.Production).RunAsync(CancellationToken.None);

        // Assert
        Assert.True(succeeded);
        await using var connection = new SqlConnection(connectionString);
        Assert.Equal(AllScripts, await ExecutedScriptsAsync(connection));
        Assert.Equal(18, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Products;"));
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Employees;"));
    }

    [Fact]
    public async Task RunAsync_ShouldFailWithoutChanges_WhenEfCoreMigrationsAreIncomplete()
    {
        // Arrange
        var connectionString = await sqlServer.CreateDatabaseAsync(NewDatabaseName());
        await CreateEfCoreDatabaseAsync(connectionString, "20261002060049_AddPinLockoutAndPinResetSubject");

        // Act
        var succeeded = await CreateMigrator(connectionString, Environments.Production).RunAsync(CancellationToken.None);

        // Assert
        Assert.False(succeeded);
        await using var connection = new SqlConnection(connectionString);
        Assert.Null(await connection.ExecuteScalarAsync<int?>("SELECT OBJECT_ID(N'dbo.SchemaVersions', N'U');"));
    }

    [Fact]
    public async Task RunAsync_ShouldSetTheTestPasswordOnlyWhereItIsMissing_WhenALocalDatabaseWasSeededBeforePasswords()
    {
        // Arrange (ITACR-47): test employees of a database seeded before 2 October 2026 have no PasswordHash.
        var connectionString = sqlServer.ConnectionStringFor(NewDatabaseName());
        var migrator = CreateMigrator(connectionString, Environments.Development);
        Assert.True(await migrator.RunAsync(CancellationToken.None));
        await using var connection = new SqlConnection(connectionString);
        await connection.ExecuteAsync("UPDATE dbo.Employees SET PasswordHash = NULL WHERE Email = 'mesero@itacr.test';");
        var ownHash = PasswordHasher.HashPassword(null!, "OtraClave2026!");
        await connection.ExecuteAsync("UPDATE dbo.Employees SET PasswordHash = @Hash WHERE Email = 'gerente@itacr.test';", new { Hash = ownHash });

        // Act
        var succeeded = await migrator.RunAsync(CancellationToken.None);

        // Assert
        Assert.True(succeeded);
        var waiterHash = await connection.ExecuteScalarAsync<string>("SELECT PasswordHash FROM dbo.Employees WHERE Email = 'mesero@itacr.test';");
        Assert.Equal(PasswordVerificationResult.Success, VerifyHash(waiterHash!, "Itacr2026!"));
        Assert.Equal(ownHash, await connection.ExecuteScalarAsync<string>("SELECT PasswordHash FROM dbo.Employees WHERE Email = 'gerente@itacr.test';"));
        Assert.Equal(4, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Employees;"));
    }

    [Fact]
    public async Task RunAsync_ShouldNotSetAnyPassword_WhenEnvironmentIsProduction()
    {
        // Arrange
        var connectionString = sqlServer.ConnectionStringFor(NewDatabaseName());
        Assert.True(await CreateMigrator(connectionString, Environments.Development).RunAsync(CancellationToken.None));
        await using var connection = new SqlConnection(connectionString);
        await connection.ExecuteAsync("UPDATE dbo.Employees SET PasswordHash = NULL;");
        var migrator = CreateMigrator(connectionString, Environments.Production, new SeedOptions { TestEmployees = true });

        // Act
        var succeeded = await migrator.RunAsync(CancellationToken.None);

        // Assert
        Assert.True(succeeded);
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Employees WHERE PasswordHash IS NOT NULL;"));
    }

    [Fact]
    public async Task RollbackAsync_ShouldReverseTheLastScriptsOneByOneAndTheNextRunShouldApplyThemAgain_WhenEnvironmentIsDevelopment()
    {
        // Arrange (ITACR-52)
        var connectionString = sqlServer.ConnectionStringFor(NewDatabaseName());
        var migrator = CreateMigrator(connectionString, Environments.Development);
        Assert.True(await migrator.RunAsync(CancellationToken.None));
        await using var connection = new SqlConnection(connectionString);

        // Act
        var resolvedAtReversed = await migrator.RollbackAsync(ResolvedAtScript, CancellationToken.None);
        var resolvedAtAfterRollback = await connection.ExecuteScalarAsync<int?>("SELECT COL_LENGTH(N'dbo.Notifications', N'ResolvedAt');");
        var modifiedAtReversed = await migrator.RollbackAsync(OrderModifiedAtScript, CancellationToken.None);
        var modifiedAtAfterRollback = await connection.ExecuteScalarAsync<int?>("SELECT COL_LENGTH(N'dbo.Orders', N'ModifiedAt');");
        var privacyReversed = await migrator.RollbackAsync(PrivacyNoticeScript, CancellationToken.None);
        var privacyAfterRollback = await connection.ExecuteScalarAsync<int?>("SELECT COL_LENGTH(N'dbo.Employees', N'PrivacyNoticeAcceptedAt');");
        var exclusionsReversed = await migrator.RollbackAsync(ExclusionOptionsScript, CancellationToken.None);
        var exclusionsAfterRollback = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.ComboOptions WHERE [Type] = N'Exclusion';");
        var indexReversed = await migrator.RollbackAsync(PushEndpointIndexScript, CancellationToken.None);
        var indexAfterRollback = await connection.ExecuteScalarAsync<int>(PushEndpointIndexQuery);
        var columnReversed = await migrator.RollbackAsync(ProductAvailabilityScript, CancellationToken.None);
        var columnAfterRollback = await connection.ExecuteScalarAsync<int?>("SELECT COL_LENGTH(N'dbo.Products', N'IsAvailable');");
        var scriptsAfterRollback = await ExecutedScriptsAsync(connection);
        var reapplied = await migrator.RunAsync(CancellationToken.None);

        // Assert
        Assert.True(resolvedAtReversed);
        Assert.Null(resolvedAtAfterRollback);
        Assert.True(modifiedAtReversed);
        Assert.Null(modifiedAtAfterRollback);
        Assert.True(privacyReversed);
        Assert.Null(privacyAfterRollback);
        Assert.True(exclusionsReversed);
        Assert.Equal(0, exclusionsAfterRollback);
        Assert.True(indexReversed);
        Assert.Equal(0, indexAfterRollback);
        Assert.True(columnReversed);
        Assert.Null(columnAfterRollback);
        Assert.Equal(AllScripts[..^6], scriptsAfterRollback);
        Assert.True(reapplied);
        Assert.Equal(AllScripts, await ExecutedScriptsAsync(connection));
        Assert.Equal(18, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Products WHERE IsAvailable = 1;"));
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(PushEndpointIndexQuery));
        Assert.Equal(7, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.ComboOptions WHERE [Type] = N'Exclusion';"));
    }

    [Fact]
    public async Task RollbackAsync_ShouldRefuseAScriptThatIsNotTheLastApplied_WhenEnvironmentIsDevelopment()
    {
        // Arrange
        var connectionString = sqlServer.ConnectionStringFor(NewDatabaseName());
        var migrator = CreateMigrator(connectionString, Environments.Development);
        Assert.True(await migrator.RunAsync(CancellationToken.None));

        // Act
        var reversed = await migrator.RollbackAsync(PickupConstraintsScript, CancellationToken.None);

        // Assert
        Assert.False(reversed);
        await using var connection = new SqlConnection(connectionString);
        Assert.Equal(AllScripts, await ExecutedScriptsAsync(connection));
    }

    [Fact]
    public async Task RollbackAsync_ShouldChangeNothing_WhenEnvironmentIsProduction()
    {
        // Arrange
        var connectionString = sqlServer.ConnectionStringFor(NewDatabaseName());
        Assert.True(await CreateMigrator(connectionString, Environments.Development).RunAsync(CancellationToken.None));

        // Act
        var reversed = await CreateMigrator(connectionString, Environments.Production).RollbackAsync(ResolvedAtScript, CancellationToken.None);

        // Assert
        Assert.False(reversed);
        await using var connection = new SqlConnection(connectionString);
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(PushEndpointIndexQuery));
        Assert.Equal(AllScripts, await ExecutedScriptsAsync(connection));
    }

    [Fact]
    public async Task RunAsync_ShouldCreateOnlyTheConfiguredManager_WhenEnvironmentIsProduction()
    {
        // Arrange
        var connectionString = await sqlServer.CreateDatabaseAsync(NewDatabaseName());
        var manager = new SeedOptions { ManagerName = "Gerente Inicial", ManagerEmail = "gerente@itacr.example", ManagerPin = "4821" };
        var migrator = CreateMigrator(connectionString, Environments.Production, manager);

        // Act
        Assert.True(await migrator.RunAsync(CancellationToken.None));
        var succeeded = await migrator.RunAsync(CancellationToken.None);

        // Assert
        Assert.True(succeeded);
        await using var connection = new SqlConnection(connectionString);
        var employee = await connection.QuerySingleAsync<(string Email, string Role, string PinHash, string? PasswordHash)>(
            "SELECT Email, Role, PinHash, PasswordHash FROM dbo.Employees;");
        Assert.Equal((manager.ManagerEmail, nameof(EmployeeRole.Manager)), (employee.Email, employee.Role));
        Assert.Equal(PasswordVerificationResult.Success, VerifyHash(employee.PinHash, "4821"));
        Assert.Null(employee.PasswordHash);
    }

    [Fact]
    public async Task RunAsync_ShouldPointEveryProductToItsWebpPhoto_WhenTheDatabaseIsMigrated()
    {
        // Arrange (ITACR-48): the photos are public/assets/images/products/<name>.webp in the frontend.
        var connectionString = sqlServer.ConnectionStringFor(NewDatabaseName());

        // Act
        Assert.True(await CreateMigrator(connectionString, Environments.Development).RunAsync(CancellationToken.None));

        // Assert
        await using var connection = new SqlConnection(connectionString);
        var products = (await connection.QueryAsync<(string Name, string? ImageUrl)>("SELECT Name, ImageUrl FROM dbo.Products;")).ToList();
        Assert.Equal(18, products.Count);
        Assert.All(products, product => Assert.Equal($"/assets/images/products/{PhotoName(product.Name)}.webp", product.ImageUrl));
        Assert.Equal(18, products.Select(product => product.ImageUrl).Distinct().Count());
    }

    [Fact]
    public async Task RunAsync_ShouldSeedTheReferenceCatalog_WhenDatabaseDoesNotExist()
    {
        // Arrange
        var connectionString = sqlServer.ConnectionStringFor(NewDatabaseName());

        // Act
        Assert.True(await CreateMigrator(connectionString, Environments.Development).RunAsync(CancellationToken.None));

        // Assert
        await using var connection = new SqlConnection(connectionString);
        Assert.Equal(Enumerable.Range(1, 10), await connection.QueryAsync<int>("SELECT Number FROM dbo.DiningTables ORDER BY Number;"));
        var options = (await connection.QueryAsync<(string Type, string Name, decimal PriceAdjustment)>(
            "SELECT Type, Name, PriceAdjustment FROM dbo.ComboOptions;")).ToList();
        Assert.Equal(3, options.Count(option => option.Type == "Size"));
        Assert.Equal(3, options.Count(option => option.Type == "Drink"));
        Assert.Equal(3, options.Count(option => option.Type == "Extra"));
        Assert.Equal(7, options.Count(option => option.Type == "Exclusion"));
        Assert.All(options.Where(option => option.Type == "Exclusion"), option => Assert.Equal(0m, option.PriceAdjustment));
        Assert.Equal(25m, options.Single(option => option.Name == "Grande").PriceAdjustment);
        var productsPerCategory = await connection.QueryAsync<int>("SELECT COUNT(*) FROM dbo.Products GROUP BY CategoryId;");
        Assert.Equal(5, productsPerCategory.Count());
        Assert.All(productsPerCategory, count => Assert.InRange(count, 3, 4));
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Products WHERE Price <= 0;"));
    }

    [Fact]
    public async Task RunAsync_ShouldFail_WhenConnectionStringIsMissing()
    {
        // Arrange
        var migrator = CreateMigrator(connectionString: null, Environments.Development);

        // Act
        var succeeded = await migrator.RunAsync(CancellationToken.None);

        // Assert
        Assert.False(succeeded);
    }

    [Fact]
    public async Task ShowStatusAsync_ShouldListTheAppliedScriptsAndNothingPending_WhenTheDatabaseIsUpToDate()
    {
        // Arrange
        var connectionString = sqlServer.ConnectionStringFor(NewDatabaseName());
        var logger = new RecordingLogger<DatabaseMigrator>();
        var migrator = CreateMigrator(connectionString, Environments.Development, logger: logger);
        Assert.True(await migrator.RunAsync(CancellationToken.None));

        // Act
        var succeeded = await migrator.ShowStatusAsync(CancellationToken.None);

        // Assert
        Assert.True(succeeded);
        Assert.All(AllScripts, script => Assert.Contains(logger.Messages, message => message.Contains(script, StringComparison.Ordinal)));
        Assert.Contains(logger.Messages, message => message.Contains($"{AllScripts.Length} versioned scripts applied, 0 pending"));
    }

    [Fact]
    public async Task ShowStatusAsync_ShouldListThePendingScript_WhenOneWasReversed()
    {
        // Arrange
        var connectionString = sqlServer.ConnectionStringFor(NewDatabaseName());
        var logger = new RecordingLogger<DatabaseMigrator>();
        var migrator = CreateMigrator(connectionString, Environments.Development, logger: logger);
        Assert.True(await migrator.RunAsync(CancellationToken.None));
        Assert.True(await migrator.RollbackAsync(ResolvedAtScript, CancellationToken.None));

        // Act
        var succeeded = await migrator.ShowStatusAsync(CancellationToken.None);

        // Assert
        Assert.True(succeeded);
        Assert.Contains(logger.Messages, message => message.StartsWith("Pending", StringComparison.Ordinal) && message.Contains(ResolvedAtScript, StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains($"{AllScripts.Length - 1} versioned scripts applied, 1 pending"));
    }

    [Fact]
    public async Task ShowStatusAsync_ShouldFailWithoutChanges_WhenTheConnectionStringIsMissing()
    {
        // Arrange
        var migrator = CreateMigrator(connectionString: null, Environments.Development);

        // Act
        var succeeded = await migrator.ShowStatusAsync(CancellationToken.None);

        // Assert
        Assert.False(succeeded);
    }

    [Fact]
    public async Task RollbackAsync_ShouldChangeNothing_WhenThereIsNoRollbackScriptForTheScript()
    {
        // Arrange
        var connectionString = sqlServer.ConnectionStringFor(NewDatabaseName());
        var migrator = CreateMigrator(connectionString, Environments.Development);
        Assert.True(await migrator.RunAsync(CancellationToken.None));

        // Act
        var reversed = await migrator.RollbackAsync(InitialSchemaScript, CancellationToken.None);

        // Assert
        Assert.False(reversed);
        await using var connection = new SqlConnection(connectionString);
        Assert.Equal(AllScripts, await ExecutedScriptsAsync(connection));
    }

    [Fact]
    public async Task RunAsync_ShouldReportTheCancellationAndFail_WhenTheTokenIsAlreadyCancelled()
    {
        // Arrange
        var connectionString = sqlServer.ConnectionStringFor(NewDatabaseName());
        var logger = new RecordingLogger<DatabaseMigrator>();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var succeeded = await CreateMigrator(connectionString, Environments.Development, logger: logger).RunAsync(cancellation.Token);

        // Assert
        Assert.False(succeeded);
        Assert.Contains(logger.Messages, message => message.Contains("cancelled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ShowStatusAsync_ShouldReportTheCancellationAndFail_WhenTheTokenIsAlreadyCancelled()
    {
        // Arrange
        var connectionString = sqlServer.ConnectionStringFor(NewDatabaseName());
        var logger = new RecordingLogger<DatabaseMigrator>();
        Assert.True(await CreateMigrator(connectionString, Environments.Development).RunAsync(CancellationToken.None));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        var succeeded = await CreateMigrator(connectionString, Environments.Development, logger: logger).ShowStatusAsync(cancellation.Token);

        // Assert
        Assert.False(succeeded);
        Assert.Contains(logger.Messages, message => message.Contains("cancelled", StringComparison.OrdinalIgnoreCase));
    }

    internal static DatabaseMigrator CreateMigrator(
        string? connectionString,
        string environmentName,
        SeedOptions? seed = null,
        ConnectionRetryPolicy? retryPolicy = null,
        ILogger<DatabaseMigrator>? logger = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [$"ConnectionStrings:{DatabaseMigrator.ConnectionStringName}"] = connectionString })
            .Build();
        var environment = new HostingEnvironment { EnvironmentName = environmentName };
        var plan = new EmployeeSeedPlan(Options.Create(seed ?? new SeedOptions()), NullLogger<EmployeeSeedPlan>.Instance);
        var seeder = new EmployeeSeeder(plan, PasswordHasher, TimeProvider.System, NullLogger<EmployeeSeeder>.Instance);
        return new DatabaseMigrator(configuration, environment, seeder, logger ?? NullLogger<DatabaseMigrator>.Instance, retryPolicy);
    }

    /// <summary>Builds the state of a database migrated by EF Core in sprint 1: the V001 schema plus the EF migrations history.</summary>
    private static async Task CreateEfCoreDatabaseAsync(string connectionString, string lastAppliedMigration)
    {
        var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Scripts", "migrations", InitialSchemaScript));
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var batch in script.Replace("\r\n", "\n").Split("\nGO\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            await connection.ExecuteAsync(batch);
        }

        await connection.ExecuteAsync(
            """
            CREATE TABLE dbo.__EFMigrationsHistory (MigrationId nvarchar(150) NOT NULL PRIMARY KEY, ProductVersion nvarchar(32) NOT NULL);
            INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion) VALUES (@LastAppliedMigration, N'10.0.12');
            """,
            new { LastAppliedMigration = lastAppliedMigration });
    }

    private static async Task<string[]> ExecutedScriptsAsync(SqlConnection connection) =>
        (await connection.QueryAsync<string>("SELECT ScriptName FROM dbo.SchemaVersions ORDER BY Id;")).ToArray();

    private static PasswordVerificationResult VerifyHash(string hash, string value) =>
        PasswordHasher.VerifyHashedPassword(null!, hash, value);

    /// <summary>The name of a photo: the product name without accents, in lowercase kebab case.</summary>
    private static string PhotoName(string productName)
    {
        var withoutAccents = new string([.. productName.Normalize(System.Text.NormalizationForm.FormD)
            .Where(character => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark)]);
        return string.Join('-', NonAlphanumeric().Split(withoutAccents.ToLowerInvariant()).Where(part => part.Length > 0));
    }

    [System.Text.RegularExpressions.GeneratedRegex("[^a-z0-9]+")]
    private static partial System.Text.RegularExpressions.Regex NonAlphanumeric();

    private static string NewDatabaseName() => $"ItacrMigratorTests_{Guid.NewGuid():N}";
}
