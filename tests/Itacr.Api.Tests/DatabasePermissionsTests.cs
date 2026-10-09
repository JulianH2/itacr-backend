using Dapper;
using Microsoft.Data.SqlClient;

namespace Itacr.Api.Tests;

/// <summary>Guards the premise of the API tests: the API user can run procedures and cannot touch tables.</summary>
public sealed class DatabasePermissionsTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const int PermissionDeniedError = 229;

    [Fact]
    public async Task ApiUser_ShouldBeDenied_WhenItReadsATableDirectly()
    {
        // Arrange
        await using var connection = new SqlConnection(Database.ApiConnectionString);

        // Act
        var exception = await Assert.ThrowsAsync<SqlException>(() => connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Employees;"));

        // Assert
        Assert.Equal(PermissionDeniedError, exception.Number);
    }

    [Fact]
    public async Task ApiUser_ShouldRunAProcedure_WhenItHasExecuteOnDbo()
    {
        // Arrange
        await using var connection = new SqlConnection(Database.ApiConnectionString);

        // Act
        var ok = await connection.ExecuteScalarAsync<int>("dbo.usp_Health_Ping", commandType: System.Data.CommandType.StoredProcedure);

        // Assert
        Assert.Equal(1, ok);
    }
}
