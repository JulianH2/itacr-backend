using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;

namespace Itacr.Database.Migrator.Tests;

/// <summary>
/// Schema rules that the EF Core model guarded until ITACR-36, now checked on the database the migrator creates.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class SchemaConventionsTests(SqlServerFixture sqlServer)
{
    private const int MoneyPrecision = 10;
    private const int MoneyScale = 2;

    [Fact]
    public async Task Schema_ShouldHaveAUniqueIndexOnlyOnClientOrderId_WhenOrdersIsCreated()
    {
        // Arrange
        await using var connection = await MigrateNewDatabaseAsync();

        // Act
        var uniqueIndexColumns = await connection.QueryAsync<string>(
            """
            SELECT STRING_AGG(c.name, ',') WITHIN GROUP (ORDER BY ic.key_ordinal)
            FROM sys.indexes AS i
            JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID(N'dbo.Orders') AND i.is_unique = 1 AND i.is_primary_key = 0
            GROUP BY i.index_id;
            """);

        // Assert
        Assert.Contains("ClientOrderId", uniqueIndexColumns);
    }

    [Theory]
    [InlineData("Orders", "Status")]
    [InlineData("Orders", "Channel")]
    [InlineData("Employees", "Role")]
    [InlineData("Employees", "Status")]
    [InlineData("Notifications", "Type")]
    [InlineData("ComboOptions", "Type")]
    public async Task Schema_ShouldStoreEnumsAsBoundedText_WhenTheColumnHoldsAnEnum(string table, string column)
    {
        // Arrange
        await using var connection = await MigrateNewDatabaseAsync();

        // Act
        var (type, maxLength) = await connection.QuerySingleAsync<(string Type, short MaxLength)>(
            "SELECT TYPE_NAME(c.user_type_id), c.max_length FROM sys.columns AS c WHERE c.object_id = OBJECT_ID(N'dbo.' + @Table) AND c.name = @Column;",
            new { Table = table, Column = column });

        // Assert
        Assert.Equal("nvarchar", type);
        Assert.True(maxLength > 0, $"dbo.{table}.{column} must not be nvarchar(max)");
    }

    [Theory]
    [InlineData("OrderItems", "UnitPrice")]
    [InlineData("OrderItems", "Subtotal")]
    [InlineData("Orders", "Total")]
    [InlineData("Products", "Price")]
    [InlineData("ComboOptions", "PriceAdjustment")]
    public async Task Schema_ShouldUseDecimal10And2_WhenTheColumnHoldsMoney(string table, string column)
    {
        // Arrange
        await using var connection = await MigrateNewDatabaseAsync();

        // Act
        var (type, precision, scale) = await connection.QuerySingleAsync<(string Type, byte Precision, byte Scale)>(
            "SELECT TYPE_NAME(c.user_type_id), c.precision, c.scale FROM sys.columns AS c WHERE c.object_id = OBJECT_ID(N'dbo.' + @Table) AND c.name = @Column;",
            new { Table = table, Column = column });

        // Assert
        Assert.Equal(("decimal", MoneyPrecision, MoneyScale), (type, (int)precision, (int)scale));
    }

    [Fact]
    public async Task Schema_ShouldCascadeDeletesOnlyFromOrders_WhenForeignKeysAreCreated()
    {
        // Arrange
        await using var connection = await MigrateNewDatabaseAsync();

        // Act
        var cascades = await connection.QueryAsync<(string Child, string Parent)>(
            """
            SELECT OBJECT_NAME(fk.parent_object_id), OBJECT_NAME(fk.referenced_object_id)
            FROM sys.foreign_keys AS fk
            WHERE fk.delete_referential_action_desc = N'CASCADE'
            ORDER BY 1;
            """);

        // Assert
        Assert.Equal([("OrderItems", "Orders"), ("OrderNotes", "Orders")], cascades);
    }

    private async Task<SqlConnection> MigrateNewDatabaseAsync()
    {
        var connectionString = sqlServer.ConnectionStringFor($"ItacrSchemaTests_{Guid.NewGuid():N}");
        Assert.True(await DatabaseMigratorTests.CreateMigrator(connectionString, Environments.Development).RunAsync(CancellationToken.None));
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
