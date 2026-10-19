using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Itacr.Api.Tests.Auth;
using Itacr.Domain.Identity;
using Microsoft.Data.SqlClient;

namespace Itacr.Api.Tests.Kitchen;

/// <summary>HU_04: the kitchen board reads every active order in one queue. Runs against the real procedure, as an EXECUTE-only user.</summary>
public sealed class KitchenQueueTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string QueueRoute = "/api/v1/kitchen/queue";
    private const string OrdersRoute = "/api/v1/orders";

    private static readonly Guid TableFive = Guid.Parse("40000000-0000-0000-0000-000000000005");
    private static readonly Guid TableTwo = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid ClassicBurger = Guid.Parse("20000000-0000-0000-0000-000000000005");
    private static readonly Guid Soda = Guid.Parse("20000000-0000-0000-0000-000000000012");

    [Fact]
    public async Task Get_ShouldReturnWaiterAndPickupOrdersInOneQueueOldestFirst_WhenBothChannelsHaveActiveOrders()
    {
        // Arrange (CA2)
        await using var factory = new AuthApiFactory(Database);
        var now = factory.Time.GetUtcNow().UtcDateTime;
        await AddOrderAsync("DineIn", "New", TableFive, now.AddMinutes(-6), new OrderLine(ClassicBurger, 1));
        await AddOrderAsync("MobileApp", "New", null, now.AddMinutes(-9), new OrderLine(Soda, 2));
        await AddOrderAsync("TakeAway", "InPreparation", null, now.AddMinutes(-3), new OrderLine(Soda, 1));
        using var client = await SignInAsync(factory, EmployeeRole.Kitchen);

        // Act
        var queue = await client.GetFromJsonAsync<JsonElement>(QueueRoute);

        // Assert
        var orders = queue.GetProperty("orders").EnumerateArray().ToList();
        Assert.Equal(["MobileApp", "DineIn", "TakeAway"], orders.Select(order => order.GetProperty("channel").GetString()));
        Assert.Equal(["New", "New", "InPreparation"], orders.Select(order => order.GetProperty("status").GetString()));
        Assert.Equal(5, orders[1].GetProperty("tableNumber").GetInt32());
        Assert.Equal(JsonValueKind.Null, orders[0].GetProperty("tableNumber").ValueKind);
        Assert.True(orders[0].GetProperty("orderNumber").GetInt32() > 0);
    }

    [Fact]
    public async Task Get_ShouldSeparateOrdersDeliveredAlreadyFromTheQueue_WhenThereAreDeliveredOrders()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var now = factory.Time.GetUtcNow().UtcDateTime;
        await AddOrderAsync("DineIn", "Delivered", TableFive, now.AddMinutes(-30), new OrderLine(Soda, 1));
        await AddOrderAsync("DineIn", "Ready", TableTwo, now.AddMinutes(-12), new OrderLine(Soda, 1));
        using var client = await SignInAsync(factory, EmployeeRole.Kitchen);

        // Act
        var queue = await client.GetFromJsonAsync<JsonElement>(QueueRoute);

        // Assert
        var order = Assert.Single(queue.GetProperty("orders").EnumerateArray());
        Assert.Equal("Ready", order.GetProperty("status").GetString());
        Assert.Equal(2, order.GetProperty("tableNumber").GetInt32());
    }

    [Fact]
    public async Task Get_ShouldReturnTheProductsWithTheirCustomization_WhenTheOrderHasItems()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var now = factory.Time.GetUtcNow().UtcDateTime;
        await AddOrderAsync(
            "DineIn",
            "New",
            TableFive,
            now.AddMinutes(-1),
            new OrderLine(ClassicBurger, 2, "Grande", "Refresco de Cola", """["Queso extra","Tocino"]""", """["Cebolla"]"""),
            new OrderLine(Soda, 1));
        using var client = await SignInAsync(factory, EmployeeRole.Kitchen);

        // Act
        var queue = await client.GetFromJsonAsync<JsonElement>(QueueRoute);

        // Assert
        var items = Assert.Single(queue.GetProperty("orders").EnumerateArray()).GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(["Hamburguesa Clásica", "Refresco de Cola"], items.Select(item => item.GetProperty("productName").GetString()));
        Assert.Equal(2, items[0].GetProperty("quantity").GetInt32());
        Assert.Equal("Grande", items[0].GetProperty("size").GetString());
        Assert.Equal("Refresco de Cola", items[0].GetProperty("drink").GetString());
        Assert.Equal(["Queso extra", "Tocino"], items[0].GetProperty("extras").EnumerateArray().Select(extra => extra.GetString()));
        Assert.Equal(["Cebolla"], items[0].GetProperty("exclusions").EnumerateArray().Select(exclusion => exclusion.GetString()));
        Assert.Equal(JsonValueKind.Null, items[1].GetProperty("size").ValueKind);
        Assert.Empty(items[1].GetProperty("extras").EnumerateArray());
    }

    [Fact]
    public async Task Get_ShouldReturnAnEmptyQueueWithTheServerTime_WhenThereAreNoActiveOrders()
    {
        // Arrange (CA4)
        await using var factory = new AuthApiFactory(Database);
        using var client = await SignInAsync(factory, EmployeeRole.Kitchen);

        // Act
        var queue = await client.GetFromJsonAsync<JsonElement>(QueueRoute);

        // Assert
        Assert.Empty(queue.GetProperty("orders").EnumerateArray());
        Assert.Equal(factory.Time.GetUtcNow().UtcDateTime, queue.GetProperty("serverTime").GetDateTime().ToUniversalTime());
    }

    [Fact]
    public async Task Get_ShouldWriteEveryTimestampInUtc_WhenTheQueueHasOrders()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var createdAt = factory.Time.GetUtcNow().UtcDateTime.AddMinutes(-4);
        await AddOrderAsync("TakeAway", "New", null, createdAt, new OrderLine(Soda, 1));
        using var client = await SignInAsync(factory, EmployeeRole.Kitchen);

        // Act
        var queue = await client.GetFromJsonAsync<JsonElement>(QueueRoute);

        // Assert
        var order = Assert.Single(queue.GetProperty("orders").EnumerateArray());
        Assert.EndsWith("Z", order.GetProperty("createdAt").GetString());
        Assert.EndsWith("Z", queue.GetProperty("serverTime").GetString());
        Assert.Equal(createdAt, order.GetProperty("createdAt").GetDateTime().ToUniversalTime());
    }

    [Fact]
    public async Task Get_ShouldShowAnOrderSentByTheWaiter_WhenTheKitchenAsksForTheQueue()
    {
        // Arrange (CA1, API side)
        await using var factory = new AuthApiFactory(Database);
        using var waiter = await SignInAsync(factory, EmployeeRole.Waiter);
        using var kitchen = await SignInAsync(factory, EmployeeRole.Kitchen);
        var before = await kitchen.GetFromJsonAsync<JsonElement>(QueueRoute);
        Assert.Empty(before.GetProperty("orders").EnumerateArray());

        // Act
        using var sent = await waiter.PostAsJsonAsync(OrdersRoute, new { clientOrderId = Guid.NewGuid(), tableId = TableFive, takeAway = false, items = new[] { new { productId = ClassicBurger, quantity = 2 } } });
        var after = await kitchen.GetFromJsonAsync<JsonElement>(QueueRoute);

        // Assert
        Assert.Equal(HttpStatusCode.Created, sent.StatusCode);
        var order = Assert.Single(after.GetProperty("orders").EnumerateArray());
        Assert.Equal("New", order.GetProperty("status").GetString());
        Assert.Equal("DineIn", order.GetProperty("channel").GetString());
        Assert.Equal(5, order.GetProperty("tableNumber").GetInt32());
        Assert.Equal("Hamburguesa Clásica", Assert.Single(order.GetProperty("items").EnumerateArray()).GetProperty("productName").GetString());
    }

    [Theory]
    [InlineData(EmployeeRole.Kitchen)]
    [InlineData(EmployeeRole.Manager)]
    public async Task Get_ShouldAllowKitchenStaffAndManagers_WhenTheyAskForTheQueue(EmployeeRole role)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = await SignInAsync(factory, role);

        // Act
        using var response = await client.GetAsync(QueueRoute);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ShouldRefuseAWaiter_WhenTheyAskForTheQueue()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = await SignInAsync(factory, EmployeeRole.Waiter);

        // Act
        using var response = await client.GetAsync(QueueRoute);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_ShouldAnswerUnauthorized_WhenThereIsNoSession()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(QueueRoute);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<HttpClient> SignInAsync(AuthApiFactory factory, EmployeeRole role)
    {
        const string Pin = "1234";
        var employee = await factory.AddEmployeeAsync($"Empleado {role}", role, Pin);
        var client = factory.CreateClient();
        AuthApiFactory.Authorize(client, (await factory.LoginAsync(client, employee.Id, Pin)).AccessToken);
        return client;
    }

    private async Task AddOrderAsync(string channel, string status, Guid? tableId, DateTime createdAt, params OrderLine[] items)
    {
        var orderId = Guid.NewGuid();
        await using var connection = new SqlConnection(Database.ConnectionString);

        // A pickup order always has a customer (V003); each one gets its own phone so they never share an active limit.
        Guid? customerId = channel == "MobileApp" ? Guid.NewGuid() : null;
        if (customerId is not null)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.Customers (Id, Name, Phone, CreatedAt, UpdatedAt)
                VALUES (@Id, N'Cliente de Prueba', RIGHT(CONCAT(N'0000000000', ABS(CHECKSUM(NEWID()))), 10), @CreatedAt, @CreatedAt);
                """,
                new { Id = customerId, CreatedAt = createdAt });
        }

        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.Orders (Id, ClientOrderId, Channel, Status, TableId, CustomerId, Total, LastStatusChangeAt, CreatedAt, UpdatedAt)
            VALUES (@Id, NEWID(), @Channel, @Status, @TableId, @CustomerId, 0, @CreatedAt, @CreatedAt, @CreatedAt);
            """,
            new { Id = orderId, Channel = channel, Status = status, TableId = tableId, CustomerId = customerId, CreatedAt = createdAt });

        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.OrderItems (Id, OrderId, ProductId, Quantity, UnitPrice, Subtotal, Size, Drink, Extras, Exclusions, CreatedAt, UpdatedAt)
                VALUES (@Id, @OrderId, @ProductId, @Quantity, 0, 0, @Size, @Drink, @Extras, @Exclusions, @CreatedAt, @CreatedAt);
                """,
                new { Id = ItemId(index), OrderId = orderId, item.ProductId, item.Quantity, item.Size, item.Drink, item.Extras, item.Exclusions, CreatedAt = createdAt });
        }
    }

    // SQL Server sorts uniqueidentifier values by their last group first, so the items keep the order they were added in,
    // like the SequentialGuid ids the API creates.
    private static Guid ItemId(int index) => Guid.Parse($"{Guid.NewGuid().ToString()[..24]}{index:x12}");

    private sealed record OrderLine(Guid ProductId, int Quantity, string? Size = null, string? Drink = null, string Extras = "[]", string Exclusions = "[]");
}
