using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Itacr.Api.Tests.Auth;
using Itacr.Domain.Identity;
using Microsoft.Data.SqlClient;

namespace Itacr.Api.Tests.Manager;

/// <summary>HU_16: the manager's dashboard adds up the orders by channel. Runs against the real procedures, as an EXECUTE-only user.</summary>
public sealed class ManagerDashboardTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string DashboardRoute = "/api/v1/manager/dashboard";
    private const string OrdersRoute = "/api/v1/manager/orders";

    // The day of the restaurant: Mexico City is UTC-6, so 10 November runs from 06:00Z to 06:00Z of the next day.
    private const string From = "2026-11-10T06:00:00Z";
    private const string To = "2026-11-11T06:00:00Z";
    private const string Range = $"from={From}&to={To}";

    private static readonly Guid TableFive = Guid.Parse("40000000-0000-0000-0000-000000000005");

    [Fact]
    public async Task Get_ShouldAddUpTheSalesAndTheAveragePreparationTimeOfEachChannel_WhenThereAreDeliveredOrders()
    {
        // Arrange (CA1)
        await SeedTheShiftAsync();
        await using var factory = new AuthApiFactory(Database);
        var manager = await SignInManagerAsync(factory);

        // Act
        var dashboard = await manager.GetFromJsonAsync<JsonElement>($"{DashboardRoute}?{Range}");

        // Assert: sales are the total of what was delivered in the range, by the time it was delivered
        Assert.Equal(4, dashboard.GetProperty("totalOrders").GetInt32());
        Assert.Equal(4, dashboard.GetProperty("deliveredOrders").GetInt32());
        Assert.Equal(260m, dashboard.GetProperty("sales").GetDecimal());
        var channels = dashboard.GetProperty("channels").EnumerateArray().ToList();
        Assert.Equal(["Waiter", "Pickup"], channels.Select(channel => channel.GetProperty("channel").GetString()));
        var waiter = channels[0];
        Assert.Equal((2, 2, 900, 3, 180m), (waiter.GetProperty("orders").GetInt32(), waiter.GetProperty("preparedOrders").GetInt32(), waiter.GetProperty("averagePreparationSeconds").GetInt32(), waiter.GetProperty("deliveredOrders").GetInt32(), waiter.GetProperty("sales").GetDecimal()));
        var pickup = channels[1];
        Assert.Equal((2, 1, 1800, 1, 80m), (pickup.GetProperty("orders").GetInt32(), pickup.GetProperty("preparedOrders").GetInt32(), pickup.GetProperty("averagePreparationSeconds").GetInt32(), pickup.GetProperty("deliveredOrders").GetInt32(), pickup.GetProperty("sales").GetDecimal()));
    }

    [Fact]
    public async Task Get_ShouldPointToTheChannelWithTheLongerAverage_WhenBothChannelsPreparedOrders()
    {
        // Arrange (CA3): the waiter channel takes 15 minutes, the pickup channel 30
        await SeedTheShiftAsync();
        await using var factory = new AuthApiFactory(Database);
        var manager = await SignInManagerAsync(factory);

        // Act
        var dashboard = await manager.GetFromJsonAsync<JsonElement>($"{DashboardRoute}?{Range}");

        // Assert
        Assert.Equal("Pickup", dashboard.GetProperty("slowestChannel").GetString());
    }

    [Fact]
    public async Task Get_ShouldNotPointToAnyChannel_WhenOnlyOneChannelPreparedOrders()
    {
        // Arrange
        await InsertOrderAsync("DineIn", "Ready", 100m, "2026-11-10T12:00:00Z", readyAt: "2026-11-10T12:10:00Z");
        await using var factory = new AuthApiFactory(Database);
        var manager = await SignInManagerAsync(factory);

        // Act
        var dashboard = await manager.GetFromJsonAsync<JsonElement>($"{DashboardRoute}?{Range}");

        // Assert: there is nothing to compare
        Assert.Equal(JsonValueKind.Null, dashboard.GetProperty("slowestChannel").ValueKind);
    }

    [Fact]
    public async Task Get_ShouldNotPointToAnyChannel_WhenBothTakeTheSameTime()
    {
        // Arrange
        await InsertOrderAsync("DineIn", "Ready", 100m, "2026-11-10T12:00:00Z", readyAt: "2026-11-10T12:10:00Z");
        await InsertOrderAsync("MobileApp", "Ready", 80m, "2026-11-10T13:00:00Z", readyAt: "2026-11-10T13:10:00Z");
        await using var factory = new AuthApiFactory(Database);
        var manager = await SignInManagerAsync(factory);

        // Act
        var dashboard = await manager.GetFromJsonAsync<JsonElement>($"{DashboardRoute}?{Range}");

        // Assert
        Assert.Equal(JsonValueKind.Null, dashboard.GetProperty("slowestChannel").ValueKind);
    }

    [Fact]
    public async Task Get_ShouldNotCountAnOrderThatIsNotReady_WhenItAveragesThePreparationTime()
    {
        // Arrange: a New order, and one that went back from Ready (ReadyAt cleared), have no preparation time yet
        await InsertOrderAsync("DineIn", "New", 100m, "2026-11-10T12:00:00Z");
        await InsertOrderAsync("DineIn", "InPreparation", 100m, "2026-11-10T12:05:00Z");
        await InsertOrderAsync("DineIn", "Ready", 100m, "2026-11-10T12:10:00Z", readyAt: "2026-11-10T12:12:00Z");
        await using var factory = new AuthApiFactory(Database);
        var manager = await SignInManagerAsync(factory);

        // Act
        var dashboard = await manager.GetFromJsonAsync<JsonElement>($"{DashboardRoute}?{Range}");

        // Assert
        var waiter = dashboard.GetProperty("channels")[0];
        Assert.Equal(3, waiter.GetProperty("orders").GetInt32());
        Assert.Equal(1, waiter.GetProperty("preparedOrders").GetInt32());
        Assert.Equal(120, waiter.GetProperty("averagePreparationSeconds").GetInt32());
    }

    [Fact]
    public async Task Get_ShouldGiveOneHourlyPointForEachHourWithOrdersOrSales()
    {
        // Arrange
        await SeedTheShiftAsync();
        await using var factory = new AuthApiFactory(Database);
        var manager = await SignInManagerAsync(factory);

        // Act
        var dashboard = await manager.GetFromJsonAsync<JsonElement>($"{DashboardRoute}?{Range}");

        // Assert: 06:00 has the sale of an order created before the range, 12:00 the orders created, 13:00 the deliveries
        var trend = dashboard.GetProperty("trend").EnumerateArray()
            .Select(point => (point.GetProperty("hourStart").GetDateTime().ToUniversalTime().ToString("HH:mm"), point.GetProperty("orders").GetInt32(), point.GetProperty("sales").GetDecimal()))
            .ToList();
        Assert.Equal([("06:00", 0, 30m), ("12:00", 3, 100m), ("13:00", 0, 130m), ("14:00", 1, 0m)], trend);
    }

    [Fact]
    public async Task Get_ShouldAnswerBothChannelsWithZeros_WhenTheRangeHasNoOrders()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var manager = await SignInManagerAsync(factory);

        // Act
        var dashboard = await manager.GetFromJsonAsync<JsonElement>($"{DashboardRoute}?{Range}");

        // Assert
        Assert.Equal(0, dashboard.GetProperty("totalOrders").GetInt32());
        Assert.Equal(0m, dashboard.GetProperty("sales").GetDecimal());
        Assert.Equal(2, dashboard.GetProperty("channels").GetArrayLength());
        Assert.All(dashboard.GetProperty("channels").EnumerateArray(), channel => Assert.Equal(JsonValueKind.Null, channel.GetProperty("averagePreparationSeconds").ValueKind));
        Assert.Equal(0, dashboard.GetProperty("trend").GetArrayLength());
    }

    [Theory]
    [InlineData("", "La fecha inicial es obligatoria.", "from")]
    [InlineData("from=2026-11-10T06:00:00Z", "La fecha final es obligatoria.", "to")]
    [InlineData("from=2026-11-11T06:00:00Z&to=2026-11-10T06:00:00Z", "La fecha inicial debe ser anterior a la final.", "from")]
    [InlineData("from=2026-11-10T06:00:00Z&to=2026-11-10T06:00:00Z", "La fecha inicial debe ser anterior a la final.", "from")]
    [InlineData("from=2026-10-01T06:00:00Z&to=2026-11-11T06:00:00Z", "El rango no puede ser mayor a 31 días.", "to")]
    public async Task Get_ShouldRejectTheRequest_WhenTheDateRangeIsNotValid(string query, string message, string field)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var manager = await SignInManagerAsync(factory);

        // Act
        using var response = await manager.GetAsync($"{DashboardRoute}?{query}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(message, problem.GetProperty("errors").GetProperty(field).EnumerateArray().Select(error => error.GetString()));
    }

    [Theory]
    [InlineData("Waiter", HttpStatusCode.Forbidden)]
    [InlineData("Kitchen", HttpStatusCode.Forbidden)]
    [InlineData("Manager", HttpStatusCode.OK)]
    public async Task Get_ShouldOnlyServeTheManager_WhenAnEmployeeAsksForTheDashboardAndTheTable(string roleName, HttpStatusCode expected)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var client = await SignInAsync(factory, Enum.Parse<EmployeeRole>(roleName));

        // Act
        using var dashboard = await client.GetAsync($"{DashboardRoute}?{Range}");
        using var orders = await client.GetAsync($"{OrdersRoute}?{Range}");

        // Assert
        Assert.Equal(expected, dashboard.StatusCode);
        Assert.Equal(expected, orders.StatusCode);
    }

    [Fact]
    public async Task Get_ShouldAnswerUnauthorized_WhenThereIsNoSession()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var dashboard = await client.GetAsync($"{DashboardRoute}?{Range}");
        using var orders = await client.GetAsync($"{OrdersRoute}?{Range}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, dashboard.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, orders.StatusCode);
    }

    [Fact]
    public async Task GetOrders_ShouldListTheOrdersOfTheRangeNewestFirst_WithTheirPreparationTime()
    {
        // Arrange
        await SeedTheShiftAsync();
        await using var factory = new AuthApiFactory(Database);
        var manager = await SignInManagerAsync(factory);

        // Act
        var page = await manager.GetFromJsonAsync<JsonElement>($"{OrdersRoute}?{Range}");

        // Assert
        Assert.Equal(4, page.GetProperty("totalCount").GetInt32());
        var items = page.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(["MobileApp", "TakeAway", "MobileApp", "DineIn"], items.Select(item => item.GetProperty("channel").GetString()));
        Assert.Equal([null, 1200, 1800, 600], items.Select(item => item.GetProperty("preparationSeconds").ValueKind == JsonValueKind.Null ? (int?)null : item.GetProperty("preparationSeconds").GetInt32()));
        Assert.Equal(5, items[3].GetProperty("tableNumber").GetInt32());
        Assert.Equal(100m, items[3].GetProperty("total").GetDecimal());
    }

    [Theory]
    [InlineData("Pickup", new[] { "MobileApp", "MobileApp" })]
    [InlineData("Waiter", new[] { "TakeAway", "DineIn" })]
    public async Task GetOrders_ShouldListOnlyTheOrdersOfTheChannel_WhenTheTableIsFilteredByChannel(string channel, string[] expected)
    {
        // Arrange (CA2)
        await SeedTheShiftAsync();
        await using var factory = new AuthApiFactory(Database);
        var manager = await SignInManagerAsync(factory);

        // Act
        var page = await manager.GetFromJsonAsync<JsonElement>($"{OrdersRoute}?{Range}&channel={channel}");

        // Assert
        Assert.Equal(expected, page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("channel").GetString()));
        Assert.Equal(expected.Length, page.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task GetOrders_ShouldListOnlyTheOrdersOfTheStatus_WhenTheTableIsFilteredByStatus()
    {
        // Arrange
        await SeedTheShiftAsync();
        await using var factory = new AuthApiFactory(Database);
        var manager = await SignInManagerAsync(factory);

        // Act
        var page = await manager.GetFromJsonAsync<JsonElement>($"{OrdersRoute}?{Range}&status=New");

        // Assert
        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
        Assert.Equal("New", page.GetProperty("items")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetOrders_ShouldGivePagesAndTheTotalOfTheOrdersThatMatch()
    {
        // Arrange
        await SeedTheShiftAsync();
        await using var factory = new AuthApiFactory(Database);
        var manager = await SignInManagerAsync(factory);

        // Act
        var second = await manager.GetFromJsonAsync<JsonElement>($"{OrdersRoute}?{Range}&page=2&pageSize=2");

        // Assert: newest first is D, B, C, A; the second page is C and A
        Assert.Equal(4, second.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, second.GetProperty("page").GetInt32());
        Assert.Equal(["DineIn", "MobileApp"], second.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("channel").GetString()).Reverse());
    }

    [Theory]
    [InlineData("channel=Everyone")]
    [InlineData("status=Cancelled")]
    [InlineData("page=0")]
    [InlineData("page=100001")]
    [InlineData("page=2147483647")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=0")]
    public async Task GetOrders_ShouldRejectTheRequest_WhenAFilterOrThePageIsNotValid(string extra)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var manager = await SignInManagerAsync(factory);

        // Act
        using var response = await manager.GetAsync($"{OrdersRoute}?{Range}&{extra}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetDetail_ShouldLetTheManagerOpenAnOrderOfTheTable_WithItsTimes()
    {
        // Arrange (CA4)
        await SeedTheShiftAsync();
        await using var factory = new AuthApiFactory(Database);
        var manager = await SignInManagerAsync(factory);
        var page = await manager.GetFromJsonAsync<JsonElement>($"{OrdersRoute}?{Range}&channel=Pickup&status=Delivered");
        var id = page.GetProperty("items")[0].GetProperty("id").GetGuid();

        // Act
        using var response = await manager.GetAsync($"/api/v1/orders/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, detail.GetProperty("id").GetGuid());
        Assert.Equal(80m, detail.GetProperty("total").GetDecimal());
        Assert.NotEqual(JsonValueKind.Null, detail.GetProperty("readyAt").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, detail.GetProperty("deliveredAt").ValueKind);
    }

    /// <summary>
    /// The shift of 10 November (06:00Z to 06:00Z). A waiter order of 100 (ready in 10 minutes), a take away order of 50 (20 minutes),
    /// a pickup order of 80 (30 minutes), a pickup order of 40 that is still New, an order of another day, and an order created before the
    /// range that was delivered inside it.
    /// </summary>
    private async Task SeedTheShiftAsync()
    {
        await InsertOrderAsync("DineIn", "Delivered", 100m, "2026-11-10T12:00:00Z", "2026-11-10T12:10:00Z", "2026-11-10T12:20:00Z", TableFive);
        await InsertOrderAsync("TakeAway", "Delivered", 50m, "2026-11-10T12:30:00Z", "2026-11-10T12:50:00Z", "2026-11-10T13:05:00Z");
        await InsertOrderAsync("MobileApp", "Delivered", 80m, "2026-11-10T12:15:00Z", "2026-11-10T12:45:00Z", "2026-11-10T13:00:00Z");
        await InsertOrderAsync("MobileApp", "New", 40m, "2026-11-10T14:00:00Z");
        await InsertOrderAsync("DineIn", "Delivered", 999m, "2026-11-09T12:00:00Z", "2026-11-09T12:05:00Z", "2026-11-09T12:30:00Z");
        await InsertOrderAsync("TakeAway", "Delivered", 30m, "2026-11-10T05:55:00Z", "2026-11-10T06:05:00Z", "2026-11-10T06:10:00Z");
    }

    private async Task InsertOrderAsync(string channel, string status, decimal total, string createdAt, string? readyAt = null, string? deliveredAt = null, Guid? tableId = null)
    {
        await using var connection = new SqlConnection(Database.ConnectionString);
        // A pickup order always has a customer (V003); each one gets its own phone.
        Guid? customerId = channel == "MobileApp" ? Guid.NewGuid() : null;
        if (customerId is not null)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.Customers (Id, Name, Phone, CreatedAt, UpdatedAt)
                VALUES (@Id, N'Cliente de Prueba', RIGHT(CONCAT(N'0000000000', ABS(CHECKSUM(NEWID()))), 10), @Now, @Now);
                """,
                new { Id = customerId, Now = Instant(createdAt) });
        }

        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.Orders (Id, ClientOrderId, Channel, Status, TableId, CustomerId, Total, LastStatusChangeAt, ReadyAt, DeliveredAt, CreatedAt, UpdatedAt)
            VALUES (NEWID(), NEWID(), @Channel, @Status, @TableId, @CustomerId, @Total, @CreatedAt, @ReadyAt, @DeliveredAt, @CreatedAt, @CreatedAt);
            """,
            new
            {
                Channel = channel,
                Status = status,
                TableId = tableId,
                CustomerId = customerId,
                Total = total,
                CreatedAt = Instant(createdAt),
                ReadyAt = readyAt is null ? (DateTime?)null : Instant(readyAt),
                DeliveredAt = deliveredAt is null ? (DateTime?)null : Instant(deliveredAt),
            });
    }

    private static DateTime Instant(string value) => DateTime.Parse(value, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);

    private static Task<HttpClient> SignInManagerAsync(AuthApiFactory factory) => SignInAsync(factory, EmployeeRole.Manager);

    private static async Task<HttpClient> SignInAsync(AuthApiFactory factory, EmployeeRole role)
    {
        const string Pin = "1234";
        var employee = await factory.AddEmployeeAsync($"Empleado {role} {Guid.NewGuid():N}", role, Pin);
        var client = factory.CreateClient();
        AuthApiFactory.Authorize(client, (await factory.LoginAsync(client, employee.Id, Pin)).AccessToken);
        return client;
    }
}
