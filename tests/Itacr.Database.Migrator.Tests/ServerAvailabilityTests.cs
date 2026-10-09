using System.Net;
using System.Net.Sockets;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;

namespace Itacr.Database.Migrator.Tests;

/// <summary>The migrator when SQL Server is not ready: it waits while it starts, and fails cleanly when it never answers.</summary>
/// <remarks>In the SQL Server collection so its extra container never starts while the shared one is busy.</remarks>
[Collection(SqlServerCollection.Name)]
public sealed class ServerAvailabilityTests
{
    private const int SqlServerPort = 1433;

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task RunAsync_ShouldReturnFalseWithoutThrowing_WhenTheServerNeverAnswers(string environmentName)
    {
        // Arrange: nothing listens on the port, so every connection is refused.
        var connectionString = $"Server=127.0.0.1,{GetFreePort()};Database=Itacr;User Id=sa;Password=unused;TrustServerCertificate=True;Connect Timeout=2";
        var migrator = DatabaseMigratorTests.CreateMigrator(
            connectionString,
            environmentName,
            retryPolicy: new ConnectionRetryPolicy(2, TimeSpan.FromMilliseconds(50)));

        // Act
        var succeeded = await migrator.RunAsync(CancellationToken.None);

        // Assert
        Assert.False(succeeded);
    }

    [Fact]
    public async Task RunAsync_ShouldWaitAndCreateTheDatabase_WhenSqlServerIsStillStarting()
    {
        // Arrange: the migrator starts before SQL Server, like "docker compose up" followed right away by the migrator.
        var port = GetFreePort();
        var password = $"Aa1!{Guid.NewGuid():N}";
        var connectionString = $"Server=127.0.0.1,{port};Database=ItacrStartupTests;User Id=sa;Password={password};TrustServerCertificate=True;Connect Timeout=5";
        var migrator = DatabaseMigratorTests.CreateMigrator(
            connectionString,
            Environments.Development,
            retryPolicy: new ConnectionRetryPolicy(60, TimeSpan.FromSeconds(2)));

        // Act
        var migration = migrator.RunAsync(CancellationToken.None);
        await using var container = await SqlServerContainers.StartAsync(builder => builder.WithPassword(password).WithPortBinding(port, SqlServerPort));
        var succeeded = await migration;

        // Assert
        Assert.True(succeeded);
        await using var connection = new SqlConnection(connectionString);
        Assert.Equal(18, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Products;"));
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
