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
    [InlineData("OrderStatusHistory", "FromStatus")]
    [InlineData("OrderStatusHistory", "ToStatus")]
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
        Assert.Equal([("OrderItems", "Orders"), ("OrderNotes", "Orders"), ("OrderStatusHistory", "Orders")], cascades);
    }

    [Fact]
    public async Task Schema_ShouldKeepTheStatusHistoryOfAnOrderInTimeOrder_WhenOrderStatusHistoryIsCreated()
    {
        // Arrange
        await using var connection = await MigrateNewDatabaseAsync();

        // Act
        var columns = await connection.QueryAsync<(string Name, bool IsNullable)>(
            "SELECT c.name, c.is_nullable FROM sys.columns AS c WHERE c.object_id = OBJECT_ID(N'dbo.OrderStatusHistory') ORDER BY c.column_id;");
        var timeOrderIndex = await connection.QuerySingleAsync<string>(
            """
            SELECT STRING_AGG(c.name, ',') WITHIN GROUP (ORDER BY ic.key_ordinal)
            FROM sys.indexes AS i
            JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.name = N'IX_OrderStatusHistory_OrderId_CreatedAt';
            """);

        // Assert
        Assert.Equal(
            [("Id", false), ("OrderId", false), ("FromStatus", true), ("ToStatus", false), ("ChangedById", true), ("CreatedAt", false), ("UpdatedAt", false)],
            columns);
        Assert.Equal("OrderId,CreatedAt", timeOrderIndex);
    }

    [Theory]
    [InlineData("abc1234567")]
    [InlineData("771123456")]
    public async Task Schema_ShouldRefuseAPhoneThatIsNotTenDigits_WhenACustomerIsSaved(string phone)
    {
        // Arrange
        await using var connection = await MigrateNewDatabaseAsync();

        // Act
        var insert = () => AddCustomerAsync(connection, phone);

        // Assert
        var error = await Assert.ThrowsAsync<SqlException>(insert);
        Assert.Contains("CK_Customers_Phone", error.Message);
    }

    [Fact]
    public async Task Schema_ShouldKeepOneCustomerPerPhone_WhenTwoCustomersUseTheSamePhone()
    {
        // Arrange
        await using var connection = await MigrateNewDatabaseAsync();
        await AddCustomerAsync(connection, "7711234567");

        // Act
        var insert = () => AddCustomerAsync(connection, "7711234567");

        // Assert
        var error = await Assert.ThrowsAsync<SqlException>(insert);
        Assert.Contains("IX_Customers_Phone", error.Message);
    }

    [Theory]
    [InlineData("MobileApp", false, false, false)]
    [InlineData("MobileApp", true, true, false)]
    [InlineData("MobileApp", true, false, true)]
    [InlineData("DineIn", true, false, false)]
    [InlineData("TakeAway", true, false, false)]
    public async Task Schema_ShouldRefuseAnOrderWhoseChannelDoesNotMatchItsParties_WhenItIsSaved(string channel, bool withCustomer, bool withTable, bool withWaiter)
    {
        // Arrange
        await using var connection = await MigrateNewDatabaseAsync();

        // Act
        var insert = () => AddOrderAsync(connection, channel, withCustomer, withTable, withWaiter);

        // Assert
        var error = await Assert.ThrowsAsync<SqlException>(insert);
        Assert.Contains("CK_Orders_ChannelParties", error.Message);
    }

    [Theory]
    [InlineData("MobileApp", true, false, false)]
    [InlineData("DineIn", false, true, false)]
    [InlineData("DineIn", false, true, true)]
    [InlineData("TakeAway", false, false, true)]
    public async Task Schema_ShouldAcceptAnOrderWhoseChannelMatchesItsParties_WhenItIsSaved(string channel, bool withCustomer, bool withTable, bool withWaiter)
    {
        // Arrange
        await using var connection = await MigrateNewDatabaseAsync();

        // Act
        await AddOrderAsync(connection, channel, withCustomer, withTable, withWaiter);

        // Assert
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Orders;"));
    }

    private static async Task AddCustomerAsync(SqlConnection connection, string phone, Guid? id = null) =>
        await connection.ExecuteAsync(
            "INSERT INTO dbo.Customers (Id, Name, Phone, CreatedAt, UpdatedAt) VALUES (@Id, N'Ana', @Phone, SYSUTCDATETIME(), SYSUTCDATETIME());",
            new { Id = id ?? Guid.NewGuid(), Phone = phone });

    private static async Task AddOrderAsync(SqlConnection connection, string channel, bool withCustomer, bool withTable, bool withWaiter)
    {
        Guid? customerId = withCustomer ? Guid.NewGuid() : null;
        if (customerId is not null)
        {
            await AddCustomerAsync(connection, "7711234567", customerId);
        }

        Guid? waiterId = withWaiter ? Guid.NewGuid() : null;
        if (waiterId is not null)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.Employees (Id, FullName, Email, Phone, Role, PinHash, Status, FailedLoginAttempts, CreatedAt, UpdatedAt)
                VALUES (@Id, N'Mesero', @Email, N'7710000000', N'Waiter', N'hash', N'Active', 0, SYSUTCDATETIME(), SYSUTCDATETIME());
                """,
                new { Id = waiterId, Email = $"{waiterId:N}@itacr.test" });
        }

        Guid? tableId = withTable ? await connection.ExecuteScalarAsync<Guid>("SELECT TOP 1 Id FROM dbo.DiningTables;") : null;
        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.Orders (Id, ClientOrderId, Channel, Status, TableId, WaiterId, CustomerId, Total, LastStatusChangeAt, CreatedAt, UpdatedAt)
            VALUES (NEWID(), NEWID(), @Channel, N'New', @TableId, @WaiterId, @CustomerId, 0, SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME());
            """,
            new { Channel = channel, TableId = tableId, WaiterId = waiterId, CustomerId = customerId });
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
