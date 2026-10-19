using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Itacr.Api.Tests.Auth;
using Itacr.Domain.Identity;
using Microsoft.Data.SqlClient;

namespace Itacr.Api.Tests.Orders;

/// <summary>HU_06: a customer without an account sends an order to pick up. Runs against the real procedures, as an EXECUTE-only user.</summary>
public sealed class PickupOrderTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string PickupRoute = "/api/v1/public/orders";
    private const string Phone = "7711234567";

    private static readonly Guid ClassicBurger = Guid.Parse("20000000-0000-0000-0000-000000000005");
    private static readonly Guid Soda = Guid.Parse("20000000-0000-0000-0000-000000000012");
    private static readonly Guid TableFive = Guid.Parse("40000000-0000-0000-0000-000000000005");

    [Fact]
    public async Task Post_ShouldSaveAPickupOrderAndAnswerWithoutPersonalData_WhenTheCustomerSendsItWithoutLogin()
    {
        // Arrange (CA1)
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        var clientOrderId = Guid.NewGuid();

        // Act
        using var response = await client.PostAsJsonAsync(PickupRoute, Body(clientOrderId, "Ana María", Phone, (ClassicBurger, 2), (Soda, 1)));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var order = JsonDocument.Parse(raw).RootElement;
        Assert.Equal(clientOrderId, order.GetProperty("clientOrderId").GetGuid());
        Assert.True(order.GetProperty("orderNumber").GetInt32() > 0);
        Assert.Equal("New", order.GetProperty("status").GetString());
        Assert.Equal(187m, order.GetProperty("total").GetDecimal());
        Assert.Equal(["Hamburguesa Clásica", "Refresco de Cola"], order.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        Assert.EndsWith("Z", order.GetProperty("createdAt").GetString());
        Assert.DoesNotContain(Phone, raw);
        Assert.DoesNotContain("Ana", raw);
        Assert.Equal(["clientOrderId", "orderNumber", "status", "total", "createdAt", "items"], order.EnumerateObject().Select(property => property.Name));

        await using var connection = new SqlConnection(Database.ConnectionString);
        var saved = await connection.QuerySingleAsync<(string Channel, string Status, Guid? TableId, Guid? WaiterId, Guid? CustomerId, decimal Total)>(
            "SELECT Channel, Status, TableId, WaiterId, CustomerId, Total FROM dbo.Orders WHERE ClientOrderId = @ClientOrderId;", new { ClientOrderId = clientOrderId });
        Assert.Equal(("MobileApp", "New", null, null, saved.CustomerId, 187m), saved);
        Assert.NotNull(saved.CustomerId);
        var customer = await connection.QuerySingleAsync<(string Name, string Phone)>("SELECT Name, Phone FROM dbo.Customers WHERE Id = @Id;", new { Id = saved.CustomerId });
        Assert.Equal(("Ana María", Phone), customer);
        Assert.Equal(2, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.OrderItems;"));
        var history = await connection.QuerySingleAsync<(string? FromStatus, string ToStatus, Guid? ChangedById)>("SELECT FromStatus, ToStatus, ChangedById FROM dbo.OrderStatusHistory;");
        Assert.Equal((null, "New", null), history);
    }

    [Fact]
    public async Task Post_ShouldPutTheOrderInTheKitchenQueueAsPickup_WhenTheCustomerSendsIt()
    {
        // Arrange (CA1)
        await using var factory = new AuthApiFactory(Database);
        using var customer = factory.CreateClient();
        using var kitchen = await SignInAsync(factory, EmployeeRole.Kitchen);

        // Act
        using var sent = await customer.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Ana", Phone, (Soda, 1)));
        var queue = await kitchen.GetFromJsonAsync<JsonElement>("/api/v1/kitchen/queue");

        // Assert
        Assert.Equal(HttpStatusCode.Created, sent.StatusCode);
        var order = Assert.Single(queue.GetProperty("orders").EnumerateArray());
        Assert.Equal("MobileApp", order.GetProperty("channel").GetString());
        Assert.Equal("New", order.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, order.GetProperty("tableNumber").ValueKind);
    }

    [Fact]
    public async Task Post_ShouldAskForThePhoneAndSaveNothing_WhenItIsMissing()
    {
        // Arrange (CA2)
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Ana María", phone: null, (Soda, 1)));

        // Assert
        Assert.Equal(["Este campo es obligatorio."], await FieldMessagesAsync(response, "phone"));
        Assert.Equal(0, await CountAsync("Orders"));
        Assert.Equal(0, await CountAsync("Customers"));
    }

    [Theory]
    [InlineData(null, Phone, "name", "Este campo es obligatorio.")]
    [InlineData("A", Phone, "name", "El nombre debe tener entre 2 y 60 caracteres.")]
    [InlineData("Ana", "771123456", "phone", "El teléfono debe tener 10 dígitos.")]
    [InlineData("Ana", "77112345ab", "phone", "El teléfono debe tener 10 dígitos.")]
    [InlineData("Ana", "0000000000", "phone", "Escribe un teléfono válido.")]
    [InlineData("Ana", "1234567890", "phone", "Escribe un teléfono válido.")]
    public async Task Post_ShouldExplainTheFieldAndSaveNothing_WhenTheNameOrThePhoneIsInvalid(string? name, string phone, string field, string message)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), name, phone, (Soda, 1)));

        // Assert
        Assert.Equal([message], await FieldMessagesAsync(response, field));
        Assert.Equal(0, await CountAsync("Orders"));
    }

    [Fact]
    public async Task Post_ShouldUseTheMenuPricesAndIgnoreWhatTheServerDecides_WhenTheRequestCarriesThem()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        var body = new
        {
            clientOrderId = Guid.NewGuid(),
            name = "Ana",
            phone = Phone,
            tableId = TableFive,
            waiterId = Guid.NewGuid(),
            channel = "DineIn",
            takeAway = false,
            items = new[] { new { productId = ClassicBurger, quantity = 1, unitPrice = 1m } },
        };

        // Act
        using var response = await client.PostAsJsonAsync(PickupRoute, body);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(79m, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("total").GetDecimal());
        await using var connection = new SqlConnection(Database.ConnectionString);
        var saved = await connection.QuerySingleAsync<(string Channel, Guid? TableId, Guid? WaiterId)>("SELECT Channel, TableId, WaiterId FROM dbo.Orders;");
        Assert.Equal(("MobileApp", null, null), saved);
    }

    [Fact]
    public async Task Post_ShouldReturnTheSameOrderWithoutDuplicating_WhenTheSameClientOrderIdIsSentAgain()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        var body = Body(Guid.NewGuid(), "Ana", Phone, (Soda, 1));
        using var first = await client.PostAsJsonAsync(PickupRoute, body);
        var firstNumber = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderNumber").GetInt32();

        // Act
        using var again = await client.PostAsJsonAsync(PickupRoute, body);

        // Assert
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(firstNumber, (await again.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderNumber").GetInt32());
        Assert.Equal(1, await CountAsync("Orders"));
        Assert.Equal(1, await CountAsync("Customers"));
    }

    [Fact]
    public async Task Post_ShouldReuseTheCustomerAndKeepTheLastName_WhenThePhoneOrdersAgain()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        using var first = await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Ana", Phone, (Soda, 1)));

        // Act
        using var second = await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Ana María", Phone, (Soda, 2)));

        // Assert
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        await using var connection = new SqlConnection(Database.ConnectionString);
        Assert.Equal(1, await CountAsync("Customers"));
        Assert.Equal("Ana María", await connection.ExecuteScalarAsync<string>("SELECT Name FROM dbo.Customers;"));
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>("SELECT COUNT(DISTINCT CustomerId) FROM dbo.Orders;"));
    }

    [Fact]
    public async Task Post_ShouldRefuseTheThirdActiveOrderOfAPhoneWithoutChangingTheCustomer_WhenItAlreadyHasTwo()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Ana", Phone, (Soda, 1)));
        await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Ana", Phone, (Soda, 1)));

        // Act
        using var third = await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Otra Persona", Phone, (Soda, 1)));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, third.StatusCode);
        var problem = await third.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("https://itacr.app/problems/pickup-order-limit", problem.GetProperty("type").GetString());
        Assert.Equal("Ya tienes 2 pedidos en curso. Espera a que los entreguemos para pedir otro.", problem.GetProperty("detail").GetString());
        await using var connection = new SqlConnection(Database.ConnectionString);
        Assert.Equal(2, await CountAsync("Orders"));
        Assert.Equal("Ana", await connection.ExecuteScalarAsync<string>("SELECT Name FROM dbo.Customers;"));
    }

    [Fact]
    public async Task Post_ShouldAcceptANewOrder_WhenAnActiveOrderOfThePhoneWasDelivered()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        var delivered = Guid.NewGuid();
        await client.PostAsJsonAsync(PickupRoute, Body(delivered, "Ana", Phone, (Soda, 1)));
        await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Ana", Phone, (Soda, 1)));
        await SetStatusAsync(delivered, "Delivered");

        // Act
        using var response = await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Ana", Phone, (Soda, 1)));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_ShouldLetOnlyTwoOrdersThrough_WhenManyRequestsOfOnePhoneArriveAtTheSameTime()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var clients = Enumerable.Range(0, 6).Select(_ => factory.CreateClient()).ToList();

        // Act
        var responses = await Task.WhenAll(clients.Select(client => client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Ana", Phone, (Soda, 1)))));

        // Assert
        var statuses = responses.Select(response => response.StatusCode).OrderBy(status => status).ToList();
        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Created, .. Enumerable.Repeat(HttpStatusCode.Conflict, 4)], statuses);
        Assert.Equal(2, await CountAsync("Orders"));
        Assert.Equal(1, await CountAsync("Customers"));
        foreach (var client in clients)
        {
            client.Dispose();
        }
    }

    [Fact]
    public async Task Post_ShouldSaveOneOrder_WhenTheSameClientOrderIdArrivesManyTimesAtTheSameTime()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var clientOrderId = Guid.NewGuid();
        var clients = Enumerable.Range(0, 5).Select(_ => factory.CreateClient()).ToList();

        // Act
        var responses = await Task.WhenAll(clients.Select(client => client.PostAsJsonAsync(PickupRoute, Body(clientOrderId, "Ana", Phone, (Soda, 1)))));

        // Assert
        var statuses = responses.Select(response => response.StatusCode).OrderBy(status => status).ToList();
        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.Created], statuses);
        Assert.Equal(1, await CountAsync("Orders"));
        foreach (var client in clients)
        {
            client.Dispose();
        }
    }

    [Fact]
    public async Task Post_ShouldRefuseMoreProductsThanThePickupLimit_WhenTheOrderHasTwentyOne()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        var body = new
        {
            clientOrderId = Guid.NewGuid(),
            name = "Ana",
            phone = Phone,
            items = Enumerable.Range(0, 21).Select(_ => new { productId = Guid.NewGuid(), quantity = 1 }).ToArray(),
        };

        // Act
        using var response = await client.PostAsJsonAsync(PickupRoute, body);

        // Assert
        Assert.Equal(["Un pedido para recoger admite hasta 20 productos."], await FieldMessagesAsync(response, "items"));
        Assert.Equal(0, await CountAsync("Orders"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public async Task Post_ShouldRefuseAQuantityOutsideOneToTen_WhenTheOrderHasIt(int quantity)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Ana", Phone, (Soda, quantity)));

        // Assert
        Assert.Equal(["Cada producto admite de 1 a 10 unidades."], await FieldMessagesAsync(response, "items[0]"));
        Assert.Equal(0, await CountAsync("Orders"));
    }

    [Fact]
    public async Task Post_ShouldRefuseAnOrderWithoutProductsOrWithAProductNotOnTheMenu()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var empty = await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Ana", Phone));
        using var unknown = await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Ana", Phone, (Guid.NewGuid(), 1)));

        // Assert
        Assert.Equal(["Agrega al menos un producto al pedido."], await FieldMessagesAsync(empty, "items"));
        Assert.Equal(["Uno o más productos ya no están en el menú."], await FieldMessagesAsync(unknown, "items"));
        Assert.Equal(0, await CountAsync("Customers"));
    }

    [Fact]
    public async Task Post_ShouldRefuseTheId_WhenItBelongsToAnOrderOfAWaiter()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var waiter = await SignInAsync(factory, EmployeeRole.Waiter);
        var clientOrderId = Guid.NewGuid();
        using var placed = await waiter.PostAsJsonAsync("/api/v1/orders", new { clientOrderId, tableId = TableFive, takeAway = false, items = new[] { new { productId = Soda, quantity = 1 } } });
        using var customer = factory.CreateClient();

        // Act
        using var response = await customer.PostAsJsonAsync(PickupRoute, Body(clientOrderId, "Ana", Phone, (Soda, 1)));

        // Assert
        Assert.Equal(HttpStatusCode.Created, placed.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await CountAsync("Orders"));
        Assert.Equal(0, await CountAsync("Customers"));
    }

    [Fact]
    public async Task Get_ShouldReturnTheCurrentStatusWithoutPersonalData_WhenTheCustomerComesBackToTheirOrder()
    {
        // Arrange (CA3)
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        var clientOrderId = Guid.NewGuid();
        await client.PostAsJsonAsync(PickupRoute, Body(clientOrderId, "Ana María", Phone, (ClassicBurger, 1)));
        await SetStatusAsync(clientOrderId, "Ready");

        // Act
        using var response = await client.GetAsync($"{PickupRoute}/{clientOrderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var order = JsonDocument.Parse(raw).RootElement;
        Assert.Equal("Ready", order.GetProperty("status").GetString());
        Assert.Equal(79m, order.GetProperty("total").GetDecimal());
        Assert.Equal("Hamburguesa Clásica", Assert.Single(order.GetProperty("items").EnumerateArray()).GetProperty("name").GetString());
        Assert.DoesNotContain(Phone, raw);
        Assert.DoesNotContain("Ana", raw);
        Assert.Equal(["clientOrderId", "orderNumber", "status", "total", "createdAt", "items"], order.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public async Task Get_ShouldAnswerNotFound_WhenTheIdDoesNotExistOrBelongsToAWaiterOrder()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var waiter = await SignInAsync(factory, EmployeeRole.Waiter);
        var waiterOrderId = Guid.NewGuid();
        await waiter.PostAsJsonAsync("/api/v1/orders", new { clientOrderId = waiterOrderId, tableId = TableFive, takeAway = false, items = new[] { new { productId = Soda, quantity = 1 } } });
        using var customer = factory.CreateClient();

        // Act
        using var unknown = await customer.GetAsync($"{PickupRoute}/{Guid.NewGuid()}");
        using var staffOrder = await customer.GetAsync($"{PickupRoute}/{waiterOrderId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, staffOrder.StatusCode);
        var unknownProblem = await unknown.Content.ReadFromJsonAsync<JsonElement>();
        var staffProblem = await staffOrder.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(unknownProblem.GetProperty("detail").GetString(), staffProblem.GetProperty("detail").GetString());
        Assert.Equal(unknownProblem.GetProperty("title").GetString(), staffProblem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Get_ShouldAnswerNotFound_WhenTheIdIsNotAGuid()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync($"{PickupRoute}/not-a-guid");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_ShouldAnswerTooManyRequests_WhenOneAddressSendsMoreOrdersThanItsLimit()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database, new Dictionary<string, string> { ["PublicOrders:CreatePermitLimit"] = "2" });
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Ana", "7711234567", (Soda, 1)));
        await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Beto", "7717654321", (Soda, 1)));

        // Act
        using var third = await client.PostAsJsonAsync(PickupRoute, Body(Guid.NewGuid(), "Carla", "7710001112", (Soda, 1)));

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.Equal(2, await CountAsync("Orders"));
    }

    [Fact]
    public async Task Get_ShouldAnswerTooManyRequests_WhenOneAddressAsksMoreOftenThanItsLimit()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database, new Dictionary<string, string> { ["PublicOrders:StatusPermitLimit"] = "2" });
        using var client = factory.CreateClient();
        var route = $"{PickupRoute}/{Guid.NewGuid()}";
        using var first = await client.GetAsync(route);
        using var second = await client.GetAsync(route);

        // Act
        using var third = await client.GetAsync(route);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task Post_ShouldKeepTheCreateAndStatusLimitsApart_WhenOneOfThemIsUsedUp()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database, new Dictionary<string, string> { ["PublicOrders:CreatePermitLimit"] = "1" });
        using var client = factory.CreateClient();
        var clientOrderId = Guid.NewGuid();
        await client.PostAsJsonAsync(PickupRoute, Body(clientOrderId, "Ana", Phone, (Soda, 1)));

        // Act
        using var status = await client.GetAsync($"{PickupRoute}/{clientOrderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
    }

    private static object Body(Guid clientOrderId, string? name, string? phone, params (Guid ProductId, int Quantity)[] items) => new
    {
        clientOrderId,
        name,
        phone,
        items = items.Select(item => new { productId = item.ProductId, quantity = item.Quantity }).ToArray(),
    };

    private static async Task<string[]> FieldMessagesAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return [.. problem.GetProperty("errors").GetProperty(field).EnumerateArray().Select(message => message.GetString() ?? string.Empty)];
    }

    private static async Task<HttpClient> SignInAsync(AuthApiFactory factory, EmployeeRole role)
    {
        const string Pin = "1234";
        var employee = await factory.AddEmployeeAsync($"Empleado {role}", role, Pin);
        var client = factory.CreateClient();
        AuthApiFactory.Authorize(client, (await factory.LoginAsync(client, employee.Id, Pin)).AccessToken);
        return client;
    }

    private async Task<int> CountAsync(string table)
    {
        await using var connection = new SqlConnection(Database.ConnectionString);
        return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM dbo.{table};");
    }

    private async Task SetStatusAsync(Guid clientOrderId, string status)
    {
        await using var connection = new SqlConnection(Database.ConnectionString);
        await connection.ExecuteAsync("UPDATE dbo.Orders SET Status = @Status WHERE ClientOrderId = @ClientOrderId;", new { Status = status, ClientOrderId = clientOrderId });
    }
}
