using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Itacr.Api.Tests.Auth;
using Itacr.Domain.Identity;

namespace Itacr.Api.Tests.Orders;

/// <summary>HU_05 CA4: the waiter opens an order and sees the kitchen's changes in its timeline, and finds the orders sent recently.</summary>
public sealed class OrderDetailTests(DatabaseFixture database) : DatabaseTest(database)
{
    private static readonly Guid TableFive = Guid.Parse("40000000-0000-0000-0000-000000000005");
    private static readonly Guid ClassicBurger = Guid.Parse("20000000-0000-0000-0000-000000000005");
    private static readonly Guid Soda = Guid.Parse("20000000-0000-0000-0000-000000000012");

    [Fact]
    public async Task Get_ShouldShowTheUpdatedStatusAndItsTimeline_WhenTheKitchenChangedTheOrder()
    {
        // Arrange (CA4)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var orderId = await PlaceOrderAsync(waiter, TableFive);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        factory.Time.Advance(TimeSpan.FromMinutes(1));
        (await kitchen.PostAsJsonAsync($"/api/v1/orders/{orderId}/status", new { status = "InPreparation" })).EnsureSuccessStatusCode();
        factory.Time.Advance(TimeSpan.FromMinutes(6));
        (await kitchen.PostAsJsonAsync($"/api/v1/orders/{orderId}/status", new { status = "Ready" })).EnsureSuccessStatusCode();
        (await kitchen.PostAsync($"/api/v1/orders/{orderId}/status/undo", content: null)).EnsureSuccessStatusCode();

        // Act
        var order = await waiter.GetFromJsonAsync<JsonElement>($"/api/v1/orders/{orderId}");

        // Assert
        Assert.Equal("InPreparation", order.GetProperty("status").GetString());
        Assert.Equal(5, order.GetProperty("tableNumber").GetInt32());
        Assert.Equal(["Hamburguesa Clásica", "Refresco de Cola"], order.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        var timeline = order.GetProperty("timeline").EnumerateArray().ToList();
        Assert.Equal(["New", "InPreparation", "Ready", "InPreparation"], timeline.Select(entry => entry.GetProperty("status").GetString()));
        Assert.Equal([false, false, false, true], timeline.Select(entry => entry.GetProperty("undone").GetBoolean()));
        Assert.True(timeline[1].GetProperty("changedAt").GetDateTime() > timeline[0].GetProperty("changedAt").GetDateTime());
    }

    [Fact]
    public async Task Get_ShouldAnswerNotFound_WhenTheOrderDoesNotExist()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);

        // Act
        using var response = await waiter.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRecent_ShouldListOnlyTheWaitersOwnOrdersNewestFirst_WhenSeveralWaitersSentOrders()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (otherWaiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var first = await PlaceOrderAsync(waiter, TableFive);
        factory.Time.Advance(TimeSpan.FromMinutes(3));
        var second = await PlaceOrderAsync(waiter, tableId: null);
        await PlaceOrderAsync(otherWaiter, TableFive);

        // Act
        var orders = await waiter.GetFromJsonAsync<JsonElement>("/api/v1/orders/recent");

        // Assert
        var recent = orders.EnumerateArray().ToList();
        Assert.Equal([second, first], recent.Select(order => order.GetProperty("id").GetGuid()));
        Assert.Equal(["TakeAway", "DineIn"], recent.Select(order => order.GetProperty("channel").GetString()));
        Assert.Equal("New", recent[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetRecent_ShouldLeaveOutOrdersOfAPreviousShift_WhenTheyAreOlderThanTwelveHours()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, waiterId) = await SignInAsync(factory, EmployeeRole.Waiter);
        await PlaceOrderAsync(waiter, TableFive);
        factory.Time.Advance(TimeSpan.FromHours(13));
        // The 12-hour session expired with the shift, so the waiter signs in again.
        AuthApiFactory.Authorize(waiter, (await factory.LoginAsync(waiter, waiterId, "1234")).AccessToken);

        // Act
        var orders = await waiter.GetFromJsonAsync<JsonElement>("/api/v1/orders/recent");

        // Assert
        Assert.Empty(orders.EnumerateArray());
    }

    private static async Task<(HttpClient Client, Guid EmployeeId)> SignInAsync(AuthApiFactory factory, EmployeeRole role)
    {
        const string Pin = "1234";
        var employee = await factory.AddEmployeeAsync($"Empleado {role}", role, Pin);
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
}
