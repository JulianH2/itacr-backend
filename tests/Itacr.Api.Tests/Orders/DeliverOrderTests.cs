using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Itacr.Api.Tests.Auth;
using Itacr.Domain.Identity;

namespace Itacr.Api.Tests.Orders;

/// <summary>HU_10: a waiter marks a Ready order as delivered and it is closed with the time of each stage. Runs against the real procedures, as an EXECUTE-only user.</summary>
public sealed class DeliverOrderTests(DatabaseFixture database) : DatabaseTest(database)
{
    private static readonly Guid TableFive = Guid.Parse("40000000-0000-0000-0000-000000000005");
    private static readonly Guid ClassicBurger = Guid.Parse("20000000-0000-0000-0000-000000000005");
    private static readonly Guid Soda = Guid.Parse("20000000-0000-0000-0000-000000000012");

    [Fact]
    public async Task Deliver_ShouldCloseTheOrderAndRecordTheTime_WhenTheOrderIsReady()
    {
        // Arrange (CA1)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var orderId = await PlaceOrderAsync(waiter);
        await MoveAsync(factory, kitchen, orderId, "InPreparation", TimeSpan.FromMinutes(1));
        await MoveAsync(factory, kitchen, orderId, "Ready", TimeSpan.FromMinutes(6));
        factory.Time.Advance(TimeSpan.FromMinutes(3));

        // Act
        using var response = await waiter.PostAsync($"/api/v1/orders/{orderId}/deliver", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var delivered = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Delivered", delivered.GetProperty("status").GetString());
        Assert.EndsWith("Z", delivered.GetProperty("deliveredAt").GetString());
        Assert.Empty((await waiter.GetFromJsonAsync<JsonElement>("/api/v1/orders/active")).EnumerateArray());
        Assert.Empty((await kitchen.GetFromJsonAsync<JsonElement>("/api/v1/kitchen/queue")).GetProperty("orders").EnumerateArray());
    }

    [Fact]
    public async Task GetDetail_ShouldShowTheTimeOfEachStage_WhenAManagerOpensADeliveredOrder()
    {
        // Arrange (CA3)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var (manager, _) = await SignInAsync(factory, EmployeeRole.Manager);
        var orderId = await PlaceOrderAsync(waiter);
        await MoveAsync(factory, kitchen, orderId, "InPreparation", TimeSpan.FromMinutes(1));
        await MoveAsync(factory, kitchen, orderId, "Ready", TimeSpan.FromMinutes(6));
        factory.Time.Advance(TimeSpan.FromMinutes(3));
        (await waiter.PostAsync($"/api/v1/orders/{orderId}/deliver", content: null)).EnsureSuccessStatusCode();

        // Act
        var order = await manager.GetFromJsonAsync<JsonElement>($"/api/v1/orders/{orderId}");

        // Assert
        Assert.Equal("Delivered", order.GetProperty("status").GetString());
        var timeline = order.GetProperty("timeline").EnumerateArray().ToList();
        Assert.Equal(["New", "InPreparation", "Ready", "Delivered"], timeline.Select(step => step.GetProperty("status").GetString()));
        var times = timeline.Select(step => step.GetProperty("changedAt").GetDateTime()).ToList();
        Assert.Equal(times, [.. times.Order()]);
        Assert.Equal(times[1] - times[0], TimeSpan.FromMinutes(1));
        Assert.Equal(times[3] - times[2], TimeSpan.FromMinutes(3));
        Assert.Equal(times[3], order.GetProperty("deliveredAt").GetDateTime());
        Assert.Equal(times[2], order.GetProperty("readyAt").GetDateTime());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Deliver_ShouldAnswerConflictAndChangeNothing_WhenTheOrderIsNotReady(bool inPreparation)
    {
        // Arrange (CA2, API side)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var orderId = await PlaceOrderAsync(waiter);
        if (inPreparation)
        {
            await MoveAsync(factory, kitchen, orderId, "InPreparation", TimeSpan.FromMinutes(1));
        }

        // Act
        using var response = await waiter.PostAsync($"/api/v1/orders/{orderId}/deliver", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("todavía no está listo", problem.GetProperty("detail").GetString());
        var order = await waiter.GetFromJsonAsync<JsonElement>($"/api/v1/orders/{orderId}");
        Assert.Equal(inPreparation ? "InPreparation" : "New", order.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, order.GetProperty("deliveredAt").ValueKind);
    }

    [Fact]
    public async Task Deliver_ShouldAnswerConflict_WhenTheOrderWasAlreadyDelivered()
    {
        // Arrange (a delivered order is final)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var orderId = await PlaceOrderAsync(waiter);
        await MoveAsync(factory, kitchen, orderId, "InPreparation", TimeSpan.FromMinutes(1));
        await MoveAsync(factory, kitchen, orderId, "Ready", TimeSpan.FromMinutes(1));
        (await waiter.PostAsync($"/api/v1/orders/{orderId}/deliver", content: null)).EnsureSuccessStatusCode();

        // Act
        using var again = await waiter.PostAsync($"/api/v1/orders/{orderId}/deliver", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        var problem = await again.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("ya fue entregado", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Deliver_ShouldAnswerNotFoundLikeAMissingOrder_WhenAnotherWaiterTookTheOrder()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (otherWaiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var orderId = await PlaceOrderAsync(otherWaiter);
        await MoveAsync(factory, kitchen, orderId, "InPreparation", TimeSpan.FromMinutes(1));
        await MoveAsync(factory, kitchen, orderId, "Ready", TimeSpan.FromMinutes(1));

        // Act
        using var foreign = await waiter.PostAsync($"/api/v1/orders/{orderId}/deliver", content: null);
        using var missing = await waiter.PostAsync($"/api/v1/orders/{Guid.NewGuid()}/deliver", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Single((await otherWaiter.GetFromJsonAsync<JsonElement>("/api/v1/orders/active")).EnumerateArray());
    }

    [Fact]
    public async Task Deliver_ShouldLetAnyWaiterHandOverAPickupOrder_WhenTheCustomerArrives()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var pickupId = await PlacePickupOrderAsync(factory, kitchen);
        await MoveAsync(factory, kitchen, pickupId, "InPreparation", TimeSpan.FromMinutes(1));
        await MoveAsync(factory, kitchen, pickupId, "Ready", TimeSpan.FromMinutes(5));
        var waiting = (await waiter.GetFromJsonAsync<JsonElement>("/api/v1/orders/pickups/ready")).EnumerateArray().ToList();

        // Act
        using var response = await waiter.PostAsync($"/api/v1/orders/{pickupId}/deliver", content: null);

        // Assert
        Assert.Equal([pickupId], waiting.Select(order => order.GetProperty("id").GetGuid()));
        Assert.Equal("MobileApp", waiting[0].GetProperty("channel").GetString());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await waiter.GetFromJsonAsync<JsonElement>("/api/v1/orders/pickups/ready")).EnumerateArray());
        Assert.Empty((await kitchen.GetFromJsonAsync<JsonElement>("/api/v1/kitchen/queue")).GetProperty("orders").EnumerateArray());
    }

    [Fact]
    public async Task GetReadyPickups_ShouldListOnlyTheReadyOnesOfCustomers_WhenOtherOrdersAreWaiting()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var ownReady = await PlaceOrderAsync(waiter);
        await MoveAsync(factory, kitchen, ownReady, "InPreparation", TimeSpan.FromMinutes(1));
        await MoveAsync(factory, kitchen, ownReady, "Ready", TimeSpan.FromMinutes(1));
        await PlacePickupOrderAsync(factory, kitchen);
        var readyPickup = await PlacePickupOrderAsync(factory, kitchen, phone: "7717654321");
        await MoveAsync(factory, kitchen, readyPickup, "InPreparation", TimeSpan.FromMinutes(1));
        await MoveAsync(factory, kitchen, readyPickup, "Ready", TimeSpan.FromMinutes(1));

        // Act
        var pickups = (await waiter.GetFromJsonAsync<JsonElement>("/api/v1/orders/pickups/ready")).EnumerateArray().ToList();

        // Assert
        Assert.Equal([readyPickup], pickups.Select(order => order.GetProperty("id").GetGuid()));
    }

    [Theory]
    [InlineData(EmployeeRole.Kitchen)]
    [InlineData(EmployeeRole.Manager)]
    public async Task Deliver_ShouldRefuseStaffWhoAreNotWaiters_WhenTheyTryToDeliver(EmployeeRole role)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (client, _) = await SignInAsync(factory, role);

        // Act
        using var deliver = await client.PostAsync($"/api/v1/orders/{Guid.NewGuid()}/deliver", content: null);
        using var pickups = await client.GetAsync("/api/v1/orders/pickups/ready");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, deliver.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, pickups.StatusCode);
    }

    [Fact]
    public async Task Deliver_ShouldAnswerUnauthorized_WhenThereIsNoSession()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var deliver = await client.PostAsync($"/api/v1/orders/{Guid.NewGuid()}/deliver", content: null);
        using var pickups = await client.GetAsync("/api/v1/orders/pickups/ready");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, deliver.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, pickups.StatusCode);
    }

    private static async Task<(HttpClient Client, Guid EmployeeId)> SignInAsync(AuthApiFactory factory, EmployeeRole role)
    {
        const string Pin = "1234";
        var employee = await factory.AddEmployeeAsync($"Empleado {role} {Guid.NewGuid():N}", role, Pin);
        var client = factory.CreateClient();
        AuthApiFactory.Authorize(client, (await factory.LoginAsync(client, employee.Id, Pin)).AccessToken);
        return (client, employee.Id);
    }

    private static async Task<Guid> PlaceOrderAsync(HttpClient waiter)
    {
        using var response = await waiter.PostAsJsonAsync("/api/v1/orders", new
        {
            clientOrderId = Guid.NewGuid(),
            tableId = TableFive,
            takeAway = false,
            items = new[] { new { productId = ClassicBurger, quantity = 1 }, new { productId = Soda, quantity = 1 } },
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    /// <summary>A customer sends a pickup order without signing in; the kitchen queue gives its id, as the customer never sees it.</summary>
    private static async Task<Guid> PlacePickupOrderAsync(AuthApiFactory factory, HttpClient kitchen, string phone = "7711234567")
    {
        using var customer = factory.CreateClient();
        using var response = await customer.PostAsJsonAsync("/api/v1/public/orders", new { clientOrderId = Guid.NewGuid(), name = "Ana", phone, items = new[] { new { productId = Soda, quantity = 1 } } });
        response.EnsureSuccessStatusCode();
        var number = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderNumber").GetInt32();
        var queue = (await kitchen.GetFromJsonAsync<JsonElement>("/api/v1/kitchen/queue")).GetProperty("orders").EnumerateArray();
        return queue.Single(order => order.GetProperty("orderNumber").GetInt32() == number).GetProperty("id").GetGuid();
    }

    private static async Task MoveAsync(AuthApiFactory factory, HttpClient kitchen, Guid orderId, string status, TimeSpan after)
    {
        factory.Time.Advance(after);
        using var response = await kitchen.PostAsJsonAsync($"/api/v1/orders/{orderId}/status", new { status });
        response.EnsureSuccessStatusCode();
    }
}
