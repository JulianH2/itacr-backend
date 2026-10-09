using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Itacr.Database.Migrator.Tests;

/// <summary>One SQL Server 2025 container (same image as compose.yaml) shared by the tests; each test uses its own database.</summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string Image = "mcr.microsoft.com/mssql/server:2025-latest";

    private readonly MsSqlContainer _container = new MsSqlBuilder(Image).Build();

    public string ConnectionStringFor(string databaseName) =>
        new SqlConnectionStringBuilder(_container.GetConnectionString()) { InitialCatalog = databaseName }.ConnectionString;

    public async Task<string> CreateDatabaseAsync(string databaseName)
    {
        await using var connection = new SqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand($"CREATE DATABASE [{databaseName}];", connection);
        await command.ExecuteNonQueryAsync();
        return ConnectionStringFor(databaseName);
    }

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server";
}
