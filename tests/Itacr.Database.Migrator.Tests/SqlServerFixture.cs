using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Itacr.Database.Migrator.Tests;

/// <summary>One SQL Server 2025 container (same image as compose.yaml) shared by the tests; each test uses its own database.</summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;

    private MsSqlContainer Container => _container ?? throw new InvalidOperationException("The SQL Server container has not started.");

    public string ConnectionStringFor(string databaseName) =>
        new SqlConnectionStringBuilder(Container.GetConnectionString()) { InitialCatalog = databaseName }.ConnectionString;

    public async Task<string> CreateDatabaseAsync(string databaseName)
    {
        await using var connection = new SqlConnection(Container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand($"CREATE DATABASE [{databaseName}];", connection);
        await command.ExecuteNonQueryAsync();
        return ConnectionStringFor(databaseName);
    }

    public async Task InitializeAsync() => _container = await SqlServerContainers.StartAsync();

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server";
}
