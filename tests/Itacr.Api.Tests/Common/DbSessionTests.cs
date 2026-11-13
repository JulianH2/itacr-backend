using Itacr.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace Itacr.Api.Tests.Common;

/// <summary>
/// The session of the database (ITACR-66): one connection per request, and no nested transactions.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class DbSessionTests(DatabaseFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldRefuseASecondTransaction_WhenOneIsAlreadyRunning()
    {
        // Arrange
        await using var session = new DbSession(new DatabaseConnection(Database.ConnectionString));

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => session.ExecuteInTransactionAsync(
            async cancellationToken =>
            {
                await session.ExecuteInTransactionAsync(_ => Task.CompletedTask, cancellationToken);
            },
            CancellationToken.None));

        // Assert
        Assert.Contains("nested transactions are not supported", exception.Message);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldFail_WhenTheServerIsNotThere()
    {
        // Arrange: a server that is not listening, so the connection cannot be opened.
        await using var session = new DbSession(new DatabaseConnection(
            "Server=localhost,1;Database=ItacrMissing;User ID=sa;Password=Itacr2026!;TrustServerCertificate=True;Connect Timeout=2"));

        // Act / Assert
        await Assert.ThrowsAsync<SqlException>(() => session.ExecuteInTransactionAsync(_ => Task.CompletedTask, CancellationToken.None));
    }
}
