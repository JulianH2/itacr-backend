using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Itacr.Api.Tests.Auth;
using Itacr.Application.Orders;
using Itacr.Domain.Identity;
using Microsoft.Data.SqlClient;

namespace Itacr.Api.Tests.Orders;

/// <summary>HU_03: the waiter sends an order to the kitchen. Runs against the real procedures, as an EXECUTE-only user.</summary>
public sealed class CreateOrderTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string OrdersRoute = "/api/v1/orders";

    private static readonly Guid TableFive = Guid.Parse("40000000-0000-0000-0000-000000000005");
    private static readonly Guid ClassicBurger = Guid.Parse("20000000-0000-0000-0000-000000000005");
    private static readonly Guid Soda = Guid.Parse("20000000-0000-0000-0000-000000000012");

    [Fact]
    public async Task Post_ShouldSaveTheOrderAsNewForTheTable_WhenTheWaiterSendsIt()
    {
        // Arrange (CA1)
        await using var factory = new AuthApiFactory(Database);
        var (client, waiterId) = await SignInWaiterAsync(factory);
        var clientOrderId = Guid.NewGuid();

        // Act
        using var response = await client.PostAsJsonAsync(OrdersRoute, OrderBody(clientOrderId, TableFive, takeAway: false, (ClassicBurger, 2), (Soda, 1)));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(order.GetProperty("orderNumber").GetInt32() > 0);
        Assert.Equal("New", order.GetProperty("status").GetString());
        Assert.Equal("DineIn", order.GetProperty("channel").GetString());
        Assert.Equal(5, order.GetProperty("tableNumber").GetInt32());
        Assert.Equal(187m, order.GetProperty("total").GetDecimal());
        Assert.Equal(["Hamburguesa Clásica", "Refresco de Cola"], order.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()));

        await using var connection = new SqlConnection(Database.ConnectionString);
        var saved = await connection.QuerySingleAsync<(string Status, string Channel, Guid WaiterId, decimal Total)>(
            "SELECT Status, Channel, WaiterId, Total FROM dbo.Orders WHERE ClientOrderId = @ClientOrderId;", new { ClientOrderId = clientOrderId });
        Assert.Equal(("New", "DineIn", waiterId, 187m), saved);
        Assert.Equal(2, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.OrderItems;"));
        var history = await connection.QuerySingleAsync<(string? FromStatus, string ToStatus, Guid ChangedById)>(
            "SELECT FromStatus, ToStatus, ChangedById FROM dbo.OrderStatusHistory;");
        Assert.Equal((null, "New", waiterId), history);
    }

    [Fact]
    public async Task Post_ShouldSaveATakeAwayOrderWithoutTable_WhenTheWaiterMarksItToTakeAway()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (client, _) = await SignInWaiterAsync(factory);

        // Act
        using var response = await client.PostAsJsonAsync(OrdersRoute, OrderBody(Guid.NewGuid(), null, takeAway: true, (Soda, 3)));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("TakeAway", order.GetProperty("channel").GetString());
        Assert.Equal(JsonValueKind.Null, order.GetProperty("tableNumber").ValueKind);
        Assert.Equal(87m, order.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Post_ShouldUseTheMenuPrices_WhenTheRequestCarriesAPrice()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (client, _) = await SignInWaiterAsync(factory);
        var body = new { clientOrderId = Guid.NewGuid(), tableId = TableFive, takeAway = false, items = new[] { new { productId = ClassicBurger, quantity = 1, unitPrice = 1m } } };

        // Act
        using var response = await client.PostAsJsonAsync(OrdersRoute, body);

        // Assert
        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(79m, order.GetProperty("total").GetDecimal());
    }

    [Theory]
    [InlineData(false, false, OrdersErrors.DestinationRequired)]
    [InlineData(true, true, OrdersErrors.DestinationAmbiguous)]
    public async Task Post_ShouldRejectWithoutSaving_WhenTheTableAndTakeAwayDoNotMatch(bool withTable, bool takeAway, string message)
    {
        // Arrange (CA2)
        await using var factory = new AuthApiFactory(Database);
        var (client, _) = await SignInWaiterAsync(factory);

        // Act
        using var response = await client.PostAsJsonAsync(OrdersRoute, OrderBody(Guid.NewGuid(), withTable ? TableFive : null, takeAway, (Soda, 1)));

        // Assert
        Assert.Equal([message], await FieldMessagesAsync(response, "tableId"));
        Assert.Equal(0, await CountOrdersAsync());
    }

    [Fact]
    public async Task Post_ShouldReject_WhenTheOrderHasNoProducts()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (client, _) = await SignInWaiterAsync(factory);

        // Act
        using var response = await client.PostAsJsonAsync(OrdersRoute, OrderBody(Guid.NewGuid(), TableFive, takeAway: false));

        // Assert
        Assert.Equal([OrdersErrors.ItemsRequired], await FieldMessagesAsync(response, "items"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public async Task Post_ShouldReject_WhenAQuantityIsOutOfRange(int quantity)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (client, _) = await SignInWaiterAsync(factory);

        // Act
        using var response = await client.PostAsJsonAsync(OrdersRoute, OrderBody(Guid.NewGuid(), TableFive, takeAway: false, (Soda, quantity)));

        // Assert
        Assert.Equal([OrdersErrors.QuantityOutOfRange], await FieldMessagesAsync(response, "items[0]"));
    }

    [Fact]
    public async Task Post_ShouldReject_WhenAProductIsNotOnTheMenu()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (client, _) = await SignInWaiterAsync(factory);

        // Act
        using var response = await client.PostAsJsonAsync(OrdersRoute, OrderBody(Guid.NewGuid(), TableFive, takeAway: false, (Soda, 1), (Guid.NewGuid(), 1)));

        // Assert
        Assert.Equal([OrdersErrors.ProductsNotFound], await FieldMessagesAsync(response, "items"));
        Assert.Equal(0, await CountOrdersAsync());
    }

    [Fact]
    public async Task Post_ShouldReject_WhenTheTableDoesNotExist()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (client, _) = await SignInWaiterAsync(factory);

        // Act
        using var response = await client.PostAsJsonAsync(OrdersRoute, OrderBody(Guid.NewGuid(), Guid.NewGuid(), takeAway: false, (Soda, 1)));

        // Assert
        Assert.Equal([OrdersErrors.TableNotFound], await FieldMessagesAsync(response, "tableId"));
    }

    [Fact]
    public async Task Post_ShouldReturnTheSavedOrder_WhenTheSameOrderIsSentAgain()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (client, _) = await SignInWaiterAsync(factory);
        var body = OrderBody(Guid.NewGuid(), TableFive, takeAway: false, (ClassicBurger, 1));

        // Act
        using var first = await client.PostAsJsonAsync(OrdersRoute, body);
        using var second = await client.PostAsJsonAsync(OrdersRoute, body);

        // Assert
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var firstOrder = await first.Content.ReadFromJsonAsync<JsonElement>();
        var secondOrder = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(firstOrder.GetProperty("id").GetGuid(), secondOrder.GetProperty("id").GetGuid());
        Assert.Equal(firstOrder.GetProperty("orderNumber").GetInt32(), secondOrder.GetProperty("orderNumber").GetInt32());
        Assert.Equal(["Hamburguesa Clásica"], secondOrder.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        Assert.Equal(1, await CountOrdersAsync());
    }

    [Fact]
    public async Task Post_ShouldSaveOneOrder_WhenTheSameOrderArrivesTwiceAtTheSameTime()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (client, _) = await SignInWaiterAsync(factory);
        var body = OrderBody(Guid.NewGuid(), TableFive, takeAway: false, (Soda, 2));

        // Act
        var responses = await Task.WhenAll(client.PostAsJsonAsync(OrdersRoute, body), client.PostAsJsonAsync(OrdersRoute, body));

        // Assert
        Assert.Equivalent(new[] { HttpStatusCode.Created, HttpStatusCode.OK }, responses.Select(response => response.StatusCode));
        Assert.Equal(1, await CountOrdersAsync());
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task Post_ShouldReturnConflict_WhenTheClientOrderIdBelongsToAnotherWaiter()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (first, _) = await SignInWaiterAsync(factory);
        var (second, _) = await SignInWaiterAsync(factory);
        var clientOrderId = Guid.NewGuid();
        using var saved = await first.PostAsJsonAsync(OrdersRoute, OrderBody(clientOrderId, TableFive, takeAway: false, (Soda, 1)));

        // Act
        using var response = await second.PostAsJsonAsync(OrdersRoute, OrderBody(clientOrderId, TableFive, takeAway: false, (Soda, 1)));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(OrdersErrors.OrderConflictType, problem.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Post_ShouldBeForbidden_WhenTheEmployeeIsNotAWaiter()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var kitchen = await factory.AddEmployeeAsync("Cocina de Prueba", EmployeeRole.Kitchen, "2222");
        using var client = factory.CreateClient();
        AuthApiFactory.Authorize(client, (await factory.LoginAsync(client, kitchen.Id, "2222")).AccessToken);

        // Act
        using var response = await client.PostAsJsonAsync(OrdersRoute, OrderBody(Guid.NewGuid(), TableFive, takeAway: false, (Soda, 1)));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_ShouldRequireAToken_WhenTheRequestIsAnonymous()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(OrdersRoute, OrderBody(Guid.NewGuid(), TableFive, takeAway: false, (Soda, 1)));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDiningTables_ShouldListTheTablesInNumberOrder_WhenTheCallerIsAWaiter()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (client, _) = await SignInWaiterAsync(factory);

        // Act
        var tables = await client.GetFromJsonAsync<JsonElement>("/api/v1/dining-tables");

        // Assert
        Assert.Equal(Enumerable.Range(1, 10), tables.EnumerateArray().Select(table => table.GetProperty("number").GetInt32()));
        Assert.Equal(TableFive, tables.EnumerateArray().Single(table => table.GetProperty("number").GetInt32() == 5).GetProperty("id").GetGuid());
    }

    private static async Task<(HttpClient Client, Guid WaiterId)> SignInWaiterAsync(AuthApiFactory factory)
    {
        var waiter = await factory.AddEmployeeAsync("Mesero de Prueba", EmployeeRole.Waiter, "1111");
        var client = factory.CreateClient();
        AuthApiFactory.Authorize(client, (await factory.LoginAsync(client, waiter.Id, "1111")).AccessToken);
        return (client, waiter.Id);
    }

    private static object OrderBody(Guid clientOrderId, Guid? tableId, bool takeAway, params (Guid ProductId, int Quantity)[] items) => new
    {
        clientOrderId,
        tableId,
        takeAway,
        items = items.Select(item => new { productId = item.ProductId, quantity = item.Quantity }).ToArray(),
    };

    private static async Task<string[]> FieldMessagesAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return [.. problem.GetProperty("errors").GetProperty(field).EnumerateArray().Select(message => message.GetString() ?? string.Empty)];
    }

    private async Task<int> CountOrdersAsync()
    {
        await using var connection = new SqlConnection(Database.ConnectionString);
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Orders;");
    }
}
