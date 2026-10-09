using Dapper;
using Itacr.Database.Migrator;
using Itacr.Database.Migrator.Seeding;
using Itacr.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.MsSql;

namespace Itacr.Api.Tests;

/// <summary>
/// One SQL Server 2025 container for every API test that needs a database, migrated once with the real migrator.
/// The catalog stays; <see cref="ResetAsync"/> deletes the rows that tests create.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string DatabaseName = "ItacrApiTests";
    private const string ApiLogin = "itacr_app_tests";

    private MsSqlContainer? _container;

    private MsSqlContainer Container => _container ?? throw new InvalidOperationException("The SQL Server container has not started.");

    /// <summary>Administrator connection, for the tests to arrange and inspect data.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>Connection of the API under test: EXECUTE only.</summary>
    public string ApiConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _container = await SqlServerContainers.StartAsync();
        ConnectionString = new SqlConnectionStringBuilder(Container.GetConnectionString()) { InitialCatalog = DatabaseName }.ConnectionString;

        await using (var master = new SqlConnection(Container.GetConnectionString()))
        {
            await master.ExecuteAsync($"CREATE DATABASE [{DatabaseName}];");
        }

        // Production: the migrator applies every script but seeds no test employees, so each test creates its own.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [$"ConnectionStrings:{DatabaseMigrator.ConnectionStringName}"] = ConnectionString })
            .Build();
        var plan = new EmployeeSeedPlan(Options.Create(new SeedOptions()), NullLogger<EmployeeSeedPlan>.Instance);
        var seeder = new EmployeeSeeder(plan, new PasswordHasher<Employee>(), TimeProvider.System, NullLogger<EmployeeSeeder>.Instance);
        var migrator = new DatabaseMigrator(
            configuration,
            new HostingEnvironment { EnvironmentName = Environments.Production },
            seeder,
            NullLogger<DatabaseMigrator>.Instance);

        if (!await migrator.RunAsync(CancellationToken.None))
        {
            throw new InvalidOperationException("The database migrator failed; see the test output.");
        }

        ApiConnectionString = await CreateExecuteOnlyUserAsync();
    }

    /// <summary>
    /// The API runs as a user with EXECUTE on dbo and nothing else, like itacr_app in production, so a query that
    /// does not go through a stored procedure fails the tests.
    /// </summary>
    private async Task<string> CreateExecuteOnlyUserAsync()
    {
        var password = $"Tests-{Guid.NewGuid():N}-Aa1!";

        await using (var master = new SqlConnection(Container.GetConnectionString()))
        {
            await master.ExecuteAsync($"CREATE LOGIN [{ApiLogin}] WITH PASSWORD = N'{password}';");
        }

        await using (var connection = new SqlConnection(ConnectionString))
        {
            await connection.ExecuteAsync($"CREATE USER [{ApiLogin}] FOR LOGIN [{ApiLogin}]; GRANT EXECUTE ON SCHEMA::dbo TO [{ApiLogin}];");
        }

        return new SqlConnectionStringBuilder(ConnectionString) { UserID = ApiLogin, Password = password }.ConnectionString;
    }

    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.ExecuteAsync(
            """
            DELETE FROM dbo.Notifications;
            DELETE FROM dbo.PushSubscriptions;
            DELETE FROM dbo.OrderNotes;
            DELETE FROM dbo.OrderItems;
            DELETE FROM dbo.Orders;
            DELETE FROM dbo.Customers;
            DELETE FROM dbo.Employees;
            """);
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}

/// <summary>Base class of the tests that use the shared database: every test starts without employees or notifications.</summary>
[Collection(DatabaseCollection.Name)]
public abstract class DatabaseTest(DatabaseFixture database) : IAsyncLifetime
{
    protected DatabaseFixture Database { get; } = database;

    public Task InitializeAsync() => Database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
