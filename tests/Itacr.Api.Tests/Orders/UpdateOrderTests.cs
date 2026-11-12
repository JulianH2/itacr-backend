using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Itacr.Api.Tests.Auth;
using Itacr.Application.Orders;
using Itacr.Domain.Identity;
using Microsoft.Data.SqlClient;

namespace Itacr.Api.Tests.Orders;

/// <summary>HU_13: a waiter modifies an order until it is Ready. Runs against the real procedures, as an EXECUTE-only user.</summary>
public sealed class UpdateOrderTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string OrdersRoute = "/api/v1/orders";
    private const string QueueRoute = "/api/v1/kitchen/queue";

    private static readonly Guid TableFive = Guid.Parse("40000000-0000-0000-0000-000000000005");
    private static readonly Guid TableTwo = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid ClassicCombo = Guid.Parse("20000000-0000-0000-0000-000000000001"); // 119.00
    private static readonly Guid ClassicBurger = Guid.Parse("20000000-0000-0000-0000-000000000005"); // 79.00
    private static readonly Guid Nuggets = Guid.Parse("20000000-0000-0000-0000-000000000011"); // 55.00
    private static readonly Guid Soda = Guid.Parse("20000000-0000-0000-0000-000000000012"); // 29.00
    private static readonly Guid LargeSize = Guid.Parse("30000000-0000-0000-0000-000000000003"); // +25
    private static readonly Guid ColaDrink = Guid.Parse("30000000-0000-0000-0000-000000000004");
    private static readonly Guid CheeseExtra = Guid.Parse("30000000-0000-0000-0000-000000000007"); // +12

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Put_ShouldShowTheNewTotalAndMarkTheKitchenCard_WhenAProductIsAdded(bool inPreparation)
    {
        // Arrange (CA1): the order is a burger and a soda, 108
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var orderId = await PlaceOrderAsync(waiter);
        if (inPreparation)
        {
            await MoveAsync(factory, kitchen, orderId, "InPreparation");
        }

        var before = await FindInQueueAsync(kitchen, orderId);
        factory.Time.Advance(TimeSpan.FromMinutes(2));

        // Act
        using var response = await waiter.PutAsJsonAsync($"{OrdersRoute}/{orderId}", Body(TableFive, Line(ClassicBurger, 1), Line(Soda, 1), Line(Nuggets, 1)));
        var detail = await waiter.GetFromJsonAsync<JsonElement>($"{OrdersRoute}/{orderId}");
        var after = await FindInQueueAsync(kitchen, orderId);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(inPreparation ? "InPreparation" : "New", detail.GetProperty("status").GetString());
        Assert.Equal(163m, detail.GetProperty("total").GetDecimal());
        Assert.Equal(163m, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("total").GetDecimal());
        Assert.Equal(3, detail.GetProperty("items").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, before.GetProperty("modifiedAt").ValueKind);
        Assert.Equal(factory.Time.GetUtcNow().UtcDateTime, after.GetProperty("modifiedAt").GetDateTimeOffset().UtcDateTime);
        Assert.EndsWith("Z", after.GetProperty("modifiedAt").GetString());
        Assert.Equal(detail.GetProperty("modifiedAt").GetDateTimeOffset(), after.GetProperty("modifiedAt").GetDateTimeOffset());
        Assert.Equal(3, after.GetProperty("items").GetArrayLength());
        Assert.Equal(before.GetProperty("createdAt").GetDateTimeOffset(), after.GetProperty("createdAt").GetDateTimeOffset());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Put_ShouldAnswerConflictAndChangeNothing_WhenTheOrderIsReadyOrDelivered(bool delivered)
    {
        // Arrange (CA2, API side)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var orderId = await PlaceOrderAsync(waiter);
        await MoveAsync(factory, kitchen, orderId, "InPreparation");
        await MoveAsync(factory, kitchen, orderId, "Ready");
        if (delivered)
        {
            (await waiter.PostAsync($"{OrdersRoute}/{orderId}/deliver", content: null)).EnsureSuccessStatusCode();
        }

        // Act
        using var response = await waiter.PutAsJsonAsync($"{OrdersRoute}/{orderId}", Body(TableTwo, Line(Soda, 5)));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(OrdersErrors.OrderNotModifiable.Detail, problem.GetProperty("detail").GetString());
        var detail = await waiter.GetFromJsonAsync<JsonElement>($"{OrdersRoute}/{orderId}");
        Assert.Equal(108m, detail.GetProperty("total").GetDecimal());
        Assert.Equal(5, detail.GetProperty("tableNumber").GetInt32());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("modifiedAt").ValueKind);
    }

    [Fact]
    public async Task Put_ShouldKeepTheOriginalOrder_WhenTheChangeIsRejected()
    {
        // Arrange (CA3, API side: a change that is not saved leaves the order as it was)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var orderId = await PlaceOrderAsync(waiter);
        var original = await waiter.GetFromJsonAsync<JsonElement>($"{OrdersRoute}/{orderId}");

        // Act
        using var response = await waiter.PutAsJsonAsync($"{OrdersRoute}/{orderId}", Body(TableTwo, Line(Soda, 5), Line(Guid.NewGuid(), 1)));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var current = await waiter.GetFromJsonAsync<JsonElement>($"{OrdersRoute}/{orderId}");
        Assert.Equal(original.GetRawText(), current.GetRawText());
        Assert.Equal(JsonValueKind.Null, (await FindInQueueAsync(kitchen, orderId)).GetProperty("modifiedAt").ValueKind);
    }

    [Fact]
    public async Task Put_ShouldRejectAnOrderWithoutProducts_WhenTheWaiterEmptiesIt()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var orderId = await PlaceOrderAsync(waiter);

        // Act
        using var response = await waiter.PutAsJsonAsync($"{OrdersRoute}/{orderId}", Body(TableFive));

        // Assert
        Assert.Equal([OrdersErrors.OrderCannotBeEmpty], await FieldMessagesAsync(response, "items"));
        var detail = await waiter.GetFromJsonAsync<JsonElement>($"{OrdersRoute}/{orderId}");
        Assert.Equal(2, detail.GetProperty("items").GetArrayLength());
        Assert.Equal(108m, detail.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Put_ShouldAnswerNotFoundLikeAMissingOrder_WhenAnotherWaiterTookTheOrder()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (owner, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (other, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var orderId = await PlaceOrderAsync(owner);

        // Act
        using var foreign = await other.PutAsJsonAsync($"{OrdersRoute}/{orderId}", Body(TableFive, Line(Soda, 9)));
        using var missing = await other.PutAsJsonAsync($"{OrdersRoute}/{Guid.NewGuid()}", Body(TableFive, Line(Soda, 9)));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var detail = await owner.GetFromJsonAsync<JsonElement>($"{OrdersRoute}/{orderId}");
        Assert.Equal(108m, detail.GetProperty("total").GetDecimal());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("modifiedAt").ValueKind);
    }

    [Fact]
    public async Task Put_ShouldAnswerNotFound_WhenTheOrderIsAPickupOrderOfACustomer()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        using var customer = factory.CreateClient();
        using var created = await customer.PostAsJsonAsync("/api/v1/public/orders", new { clientOrderId = Guid.NewGuid(), name = "Ana", phone = "7711234567", items = new[] { new { productId = Soda, quantity = 1 } } });
        created.EnsureSuccessStatusCode();
        var number = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orderNumber").GetInt32();
        var pickupId = (await kitchen.GetFromJsonAsync<JsonElement>(QueueRoute)).GetProperty("orders").EnumerateArray()
            .Single(order => order.GetProperty("orderNumber").GetInt32() == number).GetProperty("id").GetGuid();

        // Act
        using var response = await waiter.PutAsJsonAsync($"{OrdersRoute}/{pickupId}", Body(null, Line(Soda, 4)));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(29m, (await kitchen.GetFromJsonAsync<JsonElement>($"{OrdersRoute}/{pickupId}")).GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Put_ShouldUseMenuPrices_WhenTheRequestCarriesAPrice()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var orderId = await PlaceOrderAsync(waiter);

        // Act
        using var response = await waiter.PutAsJsonAsync($"{OrdersRoute}/{orderId}", new
        {
            tableId = TableFive,
            total = 1m,
            items = new[] { new { productId = Soda, quantity = 2, unitPrice = 0.01m, subtotal = 0.02m } },
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(58m, order.GetProperty("total").GetDecimal());
        var item = order.GetProperty("items")[0];
        Assert.Equal(29m, item.GetProperty("unitPrice").GetDecimal());
        Assert.Equal(58m, item.GetProperty("subtotal").GetDecimal());
    }

    [Fact]
    public async Task Put_ShouldPriceTheComboOptionsFromTheMenu_WhenTheWaiterCustomizesACombo()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var orderId = await PlaceOrderAsync(waiter);

        // Act
        using var response = await waiter.PutAsJsonAsync($"{OrdersRoute}/{orderId}", new
        {
            tableId = TableFive,
            items = new[] { new { productId = ClassicCombo, quantity = 2, sizeId = LargeSize, drinkId = ColaDrink, extraIds = new[] { CheeseExtra }, exclusionIds = Array.Empty<Guid>() } },
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(312m, order.GetProperty("total").GetDecimal()); // (119 + 25 + 12) x 2
        var item = order.GetProperty("items")[0];
        Assert.Equal("Grande", item.GetProperty("size").GetString());
        Assert.Equal(["Queso extra"], item.GetProperty("extras").EnumerateArray().Select(extra => extra.GetString()));
    }

    [Fact]
    public async Task Put_ShouldChangeTheTableAndAddNotesTheKitchenReads_WhenTheWaiterAddsNotes()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var orderId = await PlaceOrderAsync(waiter);

        // Act
        (await waiter.PutAsJsonAsync($"{OrdersRoute}/{orderId}", BodyWithNote(TableTwo, "  Sin sal  ", Line(ClassicBurger, 1), Line(Soda, 1)))).EnsureSuccessStatusCode();
        (await waiter.PutAsJsonAsync($"{OrdersRoute}/{orderId}", BodyWithNote(TableTwo, "Para compartir", Line(ClassicBurger, 1), Line(Soda, 1)))).EnsureSuccessStatusCode();
        (await waiter.PutAsJsonAsync($"{OrdersRoute}/{orderId}", Body(TableTwo, Line(ClassicBurger, 1), Line(Soda, 1)))).EnsureSuccessStatusCode();

        // Assert
        var detail = await waiter.GetFromJsonAsync<JsonElement>($"{OrdersRoute}/{orderId}");
        Assert.Equal(2, detail.GetProperty("tableNumber").GetInt32());
        Assert.Equal(["Sin sal", "Para compartir"], detail.GetProperty("notes").EnumerateArray().Select(note => note.GetString()));
        var card = await FindInQueueAsync(kitchen, orderId);
        Assert.Equal(2, card.GetProperty("tableNumber").GetInt32());
        Assert.Equal(["Sin sal", "Para compartir"], card.GetProperty("notes").EnumerateArray().Select(note => note.GetString()));
        Assert.Equal(["New"], detail.GetProperty("timeline").EnumerateArray().Select(step => step.GetProperty("status").GetString()));
    }

    [Fact]
    public async Task Put_ShouldRejectTheChange_WhenTheTableOrTheOptionsDoNotMatchTheOrder()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var dineIn = await PlaceOrderAsync(waiter);
        var takeAway = await PlaceOrderAsync(waiter, takeAway: true);

        // Act
        using var noTable = await waiter.PutAsJsonAsync($"{OrdersRoute}/{dineIn}", Body(null, Line(Soda, 1)));
        using var unknownTable = await waiter.PutAsJsonAsync($"{OrdersRoute}/{dineIn}", Body(Guid.NewGuid(), Line(Soda, 1)));
        using var tableOnTakeAway = await waiter.PutAsJsonAsync($"{OrdersRoute}/{takeAway}", Body(TableFive, Line(Soda, 1)));
        using var comboWithoutSize = await waiter.PutAsJsonAsync($"{OrdersRoute}/{dineIn}", Body(TableFive, Line(ClassicCombo, 1)));
        using var tooMany = await waiter.PutAsJsonAsync($"{OrdersRoute}/{dineIn}", Body(TableFive, Line(Soda, 100)));
        using var longNote = await waiter.PutAsJsonAsync($"{OrdersRoute}/{dineIn}", BodyWithNote(TableFive, new string('a', 501), Line(Soda, 1)));
        using var duplicated = await waiter.PutAsJsonAsync($"{OrdersRoute}/{dineIn}", Body(TableFive, Line(Soda, 1), Line(Soda, 2)));

        // Assert
        Assert.Equal([OrdersErrors.TableRequiredMessage], await FieldMessagesAsync(noTable, "tableId"));
        Assert.Equal([OrdersErrors.TableNotFound], await FieldMessagesAsync(unknownTable, "tableId"));
        Assert.Equal([OrdersErrors.TakeAwayHasNoTableMessage], await FieldMessagesAsync(tableOnTakeAway, "tableId"));
        Assert.Equal([OrdersErrors.SizeRequiredMessage], await FieldMessagesAsync(comboWithoutSize, "items[0]"));
        Assert.Equal([OrdersErrors.QuantityOutOfRange], await FieldMessagesAsync(tooMany, "items[0]"));
        Assert.Equal([OrdersErrors.NoteTooLong], await FieldMessagesAsync(longNote, "note"));
        Assert.Equal([OrdersErrors.DuplicatedProduct], await FieldMessagesAsync(duplicated, "items"));
    }

    [Fact]
    public async Task Put_ShouldKeepTheTakeAwayOrderWithoutTable_WhenTheWaiterChangesItsProducts()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var orderId = await PlaceOrderAsync(waiter, takeAway: true);

        // Act
        using var response = await waiter.PutAsJsonAsync($"{OrdersRoute}/{orderId}", Body(null, Line(Soda, 3)));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("TakeAway", order.GetProperty("channel").GetString());
        Assert.Equal(JsonValueKind.Null, order.GetProperty("tableNumber").ValueKind);
        Assert.Equal(87m, order.GetProperty("total").GetDecimal());
    }

    [Theory]
    [InlineData("Ready")]
    [InlineData("Delivered")]
    [InlineData("Other")]
    public async Task UpdateProcedure_ShouldChangeNothing_WhenTheOrderMovedOnOrBelongsToAnotherWaiterAfterTheApiRead(string scenario)
    {
        // Arrange: the status or the owner differs when the procedure runs, as when the kitchen taps between the read and the write
        await using var factory = new AuthApiFactory(Database);
        var (waiter, waiterId) = await SignInAsync(factory, EmployeeRole.Waiter);
        var orderId = await PlaceOrderAsync(waiter);
        await using var connection = new SqlConnection(Database.ConnectionString);
        if (scenario != "Other")
        {
            await connection.ExecuteAsync("UPDATE dbo.Orders SET Status = @Status WHERE Id = @Id;", new { Status = scenario, Id = orderId });
        }

        // Act
        var updated = await connection.QuerySingleAsync<bool>(
            "dbo.usp_Order_Update",
            new
            {
                Id = orderId,
                WaiterId = scenario == "Other" ? Guid.NewGuid() : waiterId,
                TableId = TableTwo,
                Total = 29m,
                Now = DateTime.UtcNow,
                NoteId = Guid.NewGuid(),
                Note = "No debe quedar",
                ItemsJson = $$"""[{"Id":"{{Guid.NewGuid()}}","ProductId":"{{Soda}}","Quantity":1,"UnitPrice":29.0,"Subtotal":29.0,"Extras":[],"Exclusions":[]}]""",
            },
            commandType: System.Data.CommandType.StoredProcedure);

        // Assert
        Assert.False(updated);
        var saved = await connection.QuerySingleAsync<(decimal Total, DateTime? ModifiedAt)>("SELECT Total, ModifiedAt FROM dbo.Orders WHERE Id = @Id;", new { Id = orderId });
        Assert.Equal((108m, null), (saved.Total, saved.ModifiedAt));
        Assert.Equal(2, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.OrderItems WHERE OrderId = @Id;", new { Id = orderId }));
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.OrderNotes;"));
    }

    [Theory]
    [InlineData(EmployeeRole.Kitchen)]
    [InlineData(EmployeeRole.Manager)]
    public async Task Put_ShouldRefuseStaffWhoAreNotWaiters_WhenTheyTryToModify(EmployeeRole role)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (client, _) = await SignInAsync(factory, role);

        // Act
        using var response = await client.PutAsJsonAsync($"{OrdersRoute}/{Guid.NewGuid()}", Body(TableFive, Line(Soda, 1)));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Put_ShouldAnswerUnauthorized_WhenThereIsNoSession()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PutAsJsonAsync($"{OrdersRoute}/{Guid.NewGuid()}", Body(TableFive, Line(Soda, 1)));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static object Line(Guid productId, int quantity) => new { productId, quantity };

    private static object Body(Guid? tableId, params object[] items) => new { tableId, items };

    private static object BodyWithNote(Guid? tableId, string note, params object[] items) => new { tableId, items, note };

    private static async Task<(HttpClient Client, Guid EmployeeId)> SignInAsync(AuthApiFactory factory, EmployeeRole role)
    {
        const string Pin = "1234";
        var employee = await factory.AddEmployeeAsync($"Empleado {role} {Guid.NewGuid():N}", role, Pin);
        var client = factory.CreateClient();
        AuthApiFactory.Authorize(client, (await factory.LoginAsync(client, employee.Id, Pin)).AccessToken);
        return (client, employee.Id);
    }

    /// <summary>A burger and a soda: 108.</summary>
    private static async Task<Guid> PlaceOrderAsync(HttpClient waiter, bool takeAway = false)
    {
        using var response = await waiter.PostAsJsonAsync(OrdersRoute, new
        {
            clientOrderId = Guid.NewGuid(),
            tableId = takeAway ? (Guid?)null : TableFive,
            takeAway,
            items = new[] { new { productId = ClassicBurger, quantity = 1 }, new { productId = Soda, quantity = 1 } },
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task MoveAsync(AuthApiFactory factory, HttpClient kitchen, Guid orderId, string status)
    {
        factory.Time.Advance(TimeSpan.FromMinutes(1));
        using var response = await kitchen.PostAsJsonAsync($"{OrdersRoute}/{orderId}/status", new { status });
        response.EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> FindInQueueAsync(HttpClient kitchen, Guid orderId) =>
        (await kitchen.GetFromJsonAsync<JsonElement>(QueueRoute)).GetProperty("orders").EnumerateArray().Single(order => order.GetProperty("id").GetGuid() == orderId);

    private static async Task<string[]> FieldMessagesAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        return [.. problem.GetProperty("errors").GetProperty(field).EnumerateArray().Select(message => message.GetString() ?? string.Empty)];
    }
}
