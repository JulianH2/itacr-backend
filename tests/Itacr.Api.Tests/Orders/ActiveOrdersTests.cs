using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Itacr.Api.Tests.Auth;
using Itacr.Domain.Identity;
using Microsoft.Data.SqlClient;

namespace Itacr.Api.Tests.Orders;

/// <summary>HU_08: the waiter sees the state of their own orders, and only theirs. Runs against the real procedures, as an EXECUTE-only user.</summary>
public sealed class ActiveOrdersTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string ActiveRoute = "/api/v1/orders/active";

    private static readonly Guid TableFive = Guid.Parse("40000000-0000-0000-0000-000000000005");
    private static readonly Guid TableTwo = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid ClassicBurger = Guid.Parse("20000000-0000-0000-0000-000000000005");
    private static readonly Guid Soda = Guid.Parse("20000000-0000-0000-0000-000000000012");

    [Fact]
    public async Task Get_ShouldListEachActiveOrderWithItsTableAndStatusNewestFirst_WhenTheWaiterHasActiveOrders()
    {
        // Arrange (CA1)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var first = await PlaceOrderAsync(waiter, TableFive);
        factory.Time.Advance(TimeSpan.FromMinutes(2));
        var second = await PlaceOrderAsync(waiter, TableTwo);
        factory.Time.Advance(TimeSpan.FromMinutes(2));
        var third = await PlaceOrderAsync(waiter, tableId: null);
        await SetStatusAsync(second, "InPreparation");
        await SetStatusAsync(first, "Ready");

        // Act
        var orders = (await waiter.GetFromJsonAsync<JsonElement>(ActiveRoute)).EnumerateArray().ToList();

        // Assert
        Assert.Equal([third, second, first], orders.Select(order => order.GetProperty("id").GetGuid()));
        Assert.Equal(["New", "InPreparation", "Ready"], orders.Select(order => order.GetProperty("status").GetString()));
        Assert.Equal(["TakeAway", "DineIn", "DineIn"], orders.Select(order => order.GetProperty("channel").GetString()));
        Assert.Equal(JsonValueKind.Null, orders[0].GetProperty("tableNumber").ValueKind);
        Assert.Equal([2, 5], orders.Skip(1).Select(order => order.GetProperty("tableNumber").GetInt32()));
        Assert.All(orders, order => Assert.True(order.GetProperty("orderNumber").GetInt32() > 0));
        Assert.All(orders, order => Assert.EndsWith("Z", order.GetProperty("lastStatusChangeAt").GetString()));
    }

    [Fact]
    public async Task Get_ShouldShowTheOrderAsReady_WhenTheKitchenMarksItReadyWhileTheWaiterKeepsAsking()
    {
        // Arrange (CA2, API side)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var orderId = await PlaceOrderAsync(waiter, TableFive);
        var before = (await waiter.GetFromJsonAsync<JsonElement>(ActiveRoute)).EnumerateArray().Single();

        // Act
        factory.Time.Advance(TimeSpan.FromMinutes(1));
        (await kitchen.PostAsJsonAsync($"/api/v1/orders/{orderId}/status", new { status = "InPreparation" })).EnsureSuccessStatusCode();
        factory.Time.Advance(TimeSpan.FromMinutes(5));
        (await kitchen.PostAsJsonAsync($"/api/v1/orders/{orderId}/status", new { status = "Ready" })).EnsureSuccessStatusCode();
        var after = (await waiter.GetFromJsonAsync<JsonElement>(ActiveRoute)).EnumerateArray().Single();

        // Assert
        Assert.Equal("New", before.GetProperty("status").GetString());
        Assert.Equal("Ready", after.GetProperty("status").GetString());
        Assert.True(after.GetProperty("lastStatusChangeAt").GetDateTime() > before.GetProperty("lastStatusChangeAt").GetDateTime());
    }

    [Fact]
    public async Task Get_ShouldLeaveTheDeliveredOrdersOut_WhenAnOrderWasDelivered()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var delivered = await PlaceOrderAsync(waiter, TableFive);
        var active = await PlaceOrderAsync(waiter, TableTwo);
        await SetStatusAsync(delivered, "Delivered");

        // Act
        var orders = (await waiter.GetFromJsonAsync<JsonElement>(ActiveRoute)).EnumerateArray().ToList();

        // Assert
        Assert.Equal([active], orders.Select(order => order.GetProperty("id").GetGuid()));
    }

    [Fact]
    public async Task Get_ShouldReturnAnEmptyList_WhenTheWaiterHasNoActiveOrders()
    {
        // Arrange (CA4)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);

        // Act
        using var response = await waiter.GetAsync(ActiveRoute);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_ShouldNotShowTheOrdersOfOtherWaitersOrOfCustomers_WhenSeveralPeopleSentOrders()
    {
        // Arrange (rule 1)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (otherWaiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var mine = await PlaceOrderAsync(waiter, TableFive);
        await PlaceOrderAsync(otherWaiter, TableTwo);
        using var customer = factory.CreateClient();
        await customer.PostAsJsonAsync("/api/v1/public/orders", new { clientOrderId = Guid.NewGuid(), name = "Ana", phone = "7711234567", items = new[] { new { productId = Soda, quantity = 1 } } });

        // Act
        var orders = (await waiter.GetFromJsonAsync<JsonElement>(ActiveRoute)).EnumerateArray().ToList();
        var otherOrders = (await otherWaiter.GetFromJsonAsync<JsonElement>(ActiveRoute)).EnumerateArray().ToList();

        // Assert
        Assert.Equal([mine], orders.Select(order => order.GetProperty("id").GetGuid()));
        Assert.Single(otherOrders);
    }

    [Fact]
    public async Task Get_ShouldKeepAnOrderOfAnEarlierShiftActive_WhenItWasNeverDelivered()
    {
        // Arrange: unlike the recent orders, the active ones have no time limit.
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var orderId = await PlaceOrderAsync(waiter, TableFive);
        await using (var connection = new SqlConnection(Database.ConnectionString))
        {
            await connection.ExecuteAsync("UPDATE dbo.Orders SET CreatedAt = DATEADD(HOUR, -13, CreatedAt) WHERE Id = @Id;", new { Id = orderId });
        }

        // Act
        var orders = (await waiter.GetFromJsonAsync<JsonElement>(ActiveRoute)).EnumerateArray().ToList();

        // Assert
        Assert.Equal([orderId], orders.Select(order => order.GetProperty("id").GetGuid()));
    }

    [Theory]
    [InlineData(EmployeeRole.Kitchen)]
    [InlineData(EmployeeRole.Manager)]
    public async Task Get_ShouldRefuseStaffWhoAreNotWaiters_WhenTheyAskForActiveOrders(EmployeeRole role)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (client, _) = await SignInAsync(factory, role);

        // Act
        using var response = await client.GetAsync(ActiveRoute);

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
        using var response = await client.GetAsync(ActiveRoute);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDetail_ShouldShowTheOrderWithoutTheWaiter_WhenTheWaiterOpensTheirOwnOrder()
    {
        // Arrange (CA3)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var orderId = await PlaceOrderAsync(waiter, TableFive);

        // Act
        using var response = await waiter.GetAsync($"/api/v1/orders/{orderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var order = JsonDocument.Parse(raw).RootElement;
        Assert.Equal(["Hamburguesa Clásica", "Refresco de Cola"], order.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        Assert.Equal(108m, order.GetProperty("total").GetDecimal());
        Assert.Equal("New", Assert.Single(order.GetProperty("timeline").EnumerateArray()).GetProperty("status").GetString());
        Assert.DoesNotContain("waiter", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDetail_ShouldAnswerNotFoundLikeAMissingOrder_WhenAWaiterOpensTheOrderOfAnotherWaiter()
    {
        // Arrange (rule 1)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (otherWaiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var orderId = await PlaceOrderAsync(otherWaiter, TableFive);

        // Act
        using var foreign = await waiter.GetAsync($"/api/v1/orders/{orderId}");
        using var missing = await waiter.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var foreignProblem = await foreign.Content.ReadFromJsonAsync<JsonElement>();
        var missingProblem = await missing.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(missingProblem.GetProperty("detail").GetString(), foreignProblem.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData(EmployeeRole.Kitchen)]
    [InlineData(EmployeeRole.Manager)]
    public async Task GetDetail_ShouldLetKitchenStaffAndManagersReadAnyOrder_WhenTheyOpenIt(EmployeeRole role)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (staff, _) = await SignInAsync(factory, role);
        var orderId = await PlaceOrderAsync(waiter, TableFive);

        // Act
        using var response = await staff.GetAsync($"/api/v1/orders/{orderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDetail_ShouldKeepThePickupOrdersOfCustomersAwayFromWaiters_WhenAWaiterKnowsTheirId()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        using var customer = factory.CreateClient();
        await customer.PostAsJsonAsync("/api/v1/public/orders", new { clientOrderId = Guid.NewGuid(), name = "Ana", phone = "7711234567", items = new[] { new { productId = Soda, quantity = 1 } } });
        var pickupId = (await kitchen.GetFromJsonAsync<JsonElement>("/api/v1/kitchen/queue")).GetProperty("orders").EnumerateArray().Single().GetProperty("id").GetGuid();

        // Act
        using var asWaiter = await waiter.GetAsync($"/api/v1/orders/{pickupId}");
        using var asKitchen = await kitchen.GetAsync($"/api/v1/orders/{pickupId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, asWaiter.StatusCode);
        Assert.Equal(HttpStatusCode.OK, asKitchen.StatusCode);
    }

    private static async Task<(HttpClient Client, Guid EmployeeId)> SignInAsync(AuthApiFactory factory, EmployeeRole role)
    {
        const string Pin = "1234";
        var employee = await factory.AddEmployeeAsync($"Empleado {role} {Guid.NewGuid():N}", role, Pin);
        var client = factory.CreateClient();
        AuthApiFactory.Authorize(client, (await factory.LoginAsync(client, employee.Id, Pin)).AccessToken);
        return (client, employee.Id);
    }

    private static async Task<Guid> PlaceOrderAsync(HttpClient waiter, Guid? tableId)
    {
        using var response = await waiter.PostAsJsonAsync("/api/v1/orders", new
        {
            clientOrderId = Guid.NewGuid(),
            tableId,
            takeAway = tableId is null,
            items = new[] { new { productId = ClassicBurger, quantity = 1 }, new { productId = Soda, quantity = 1 } },
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private async Task SetStatusAsync(Guid orderId, string status)
    {
        await using var connection = new SqlConnection(Database.ConnectionString);
        await connection.ExecuteAsync("UPDATE dbo.Orders SET Status = @Status, LastStatusChangeAt = SYSUTCDATETIME() WHERE Id = @Id;", new { Status = status, Id = orderId });
    }
}
