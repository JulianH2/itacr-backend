using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Itacr.Api.Tests.Auth;
using Itacr.Application.Orders;
using Itacr.Domain.Identity;
using Microsoft.Data.SqlClient;

namespace Itacr.Api.Tests.Orders;

/// <summary>HU_12: a combo is ordered with a size, a drink, extras, and exclusions. Runs against the real procedures, as an EXECUTE-only user.</summary>
public sealed class ComboCustomizationTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string OrdersRoute = "/api/v1/orders";
    private const string PickupRoute = "/api/v1/public/orders";
    private const string OptionsRoute = "/api/v1/catalog/combo-options";

    private static readonly Guid TableFive = Guid.Parse("40000000-0000-0000-0000-000000000005");
    private static readonly Guid ClassicCombo = Guid.Parse("20000000-0000-0000-0000-000000000001"); // 119.00
    private static readonly Guid ClassicBurger = Guid.Parse("20000000-0000-0000-0000-000000000005");

    private static readonly Guid SmallSize = Guid.Parse("30000000-0000-0000-0000-000000000001"); // +0
    private static readonly Guid LargeSize = Guid.Parse("30000000-0000-0000-0000-000000000003"); // +25
    private static readonly Guid ColaDrink = Guid.Parse("30000000-0000-0000-0000-000000000004");
    private static readonly Guid CheeseExtra = Guid.Parse("30000000-0000-0000-0000-000000000007"); // +12
    private static readonly Guid BaconExtra = Guid.Parse("30000000-0000-0000-0000-000000000008"); // +18
    private static readonly Guid NoOnion = Guid.Parse("30000000-0000-0000-0000-000000000010");
    private static readonly Guid NoTomato = Guid.Parse("30000000-0000-0000-0000-000000000011");

    [Fact]
    public async Task Get_ShouldListTheOptionsWithoutAToken_WhenAnyoneAsksForTheMenu()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(OptionsRoute);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var options = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        string[] TypeNames(string type) => [.. options.Where(option => option.GetProperty("type").GetString() == type).Select(option => option.GetProperty("name").GetString()!)];
        Assert.Equal(["Chico", "Mediano", "Grande"], TypeNames("Size"));
        Assert.Equal(3, TypeNames("Drink").Length);
        Assert.Equal(["Jalapeños", "Queso extra", "Tocino"], TypeNames("Extra"));
        Assert.Contains("Cebolla", TypeNames("Exclusion"));
        var large = options.Single(option => option.GetProperty("name").GetString() == "Grande");
        Assert.Equal(25m, large.GetProperty("priceAdjustment").GetDecimal());
        Assert.Equal(LargeSize, large.GetProperty("id").GetGuid());
        Assert.All(options.Where(option => option.GetProperty("type").GetString() == "Exclusion"), option => Assert.Equal(0m, option.GetProperty("priceAdjustment").GetDecimal()));
    }

    [Fact]
    public async Task Post_ShouldPriceTheSizeAndTheExtraAndSaveTheChoices_WhenTheWaiterCustomizesACombo()
    {
        // Arrange (CA1): grande, refresco de cola, extra queso, sin cebolla
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var clientOrderId = Guid.NewGuid();

        // Act
        using var response = await waiter.PostAsJsonAsync(OrdersRoute, WaiterBody(clientOrderId, Line(ClassicCombo, 2, LargeSize, ColaDrink, [CheeseExtra], [NoOnion])));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(312m, order.GetProperty("total").GetDecimal()); // (119 + 25 + 12) x 2
        var item = order.GetProperty("items")[0];
        Assert.Equal(156m, item.GetProperty("unitPrice").GetDecimal());
        Assert.Equal(312m, item.GetProperty("subtotal").GetDecimal());

        await using var connection = new SqlConnection(Database.ConnectionString);
        var saved = await connection.QuerySingleAsync<(string Size, string Drink, string Extras, string Exclusions, decimal UnitPrice)>(
            "SELECT Size, Drink, CAST(Extras AS nvarchar(max)) AS Extras, CAST(Exclusions AS nvarchar(max)) AS Exclusions, UnitPrice FROM dbo.OrderItems;");
        Assert.Equal(("Grande", "Refresco de cola", "Queso extra", "Cebolla", 156m), (saved.Size, saved.Drink, JsonSerializer.Deserialize<string[]>(saved.Extras)![0], JsonSerializer.Deserialize<string[]>(saved.Exclusions)![0], saved.UnitPrice));
    }

    [Fact]
    public async Task Post_ShouldNotChangeThePrice_WhenTheCustomerOnlyExcludesIngredients()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);

        // Act
        using var response = await waiter.PostAsJsonAsync(OrdersRoute, WaiterBody(Guid.NewGuid(), Line(ClassicCombo, 1, SmallSize, ColaDrink, [], [NoOnion, NoTomato])));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(119m, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Post_ShouldSaveTwoLines_WhenTheSameComboHasDifferentOptions()
    {
        // Arrange: one combo without onion and another with extra cheese
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);

        // Act
        using var response = await waiter.PostAsJsonAsync(OrdersRoute, WaiterBody(
            Guid.NewGuid(),
            Line(ClassicCombo, 1, SmallSize, ColaDrink, [], [NoOnion]),
            Line(ClassicCombo, 1, SmallSize, ColaDrink, [CheeseExtra], [])));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(250m, order.GetProperty("total").GetDecimal()); // 119 + (119 + 12)
        Assert.Equal(2, order.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Post_ShouldRejectTheOrder_WhenTwoLinesAreTheSameComboWithTheSameOptions()
    {
        // Arrange (the extras come in another order, which is still the same line)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);

        // Act
        using var response = await waiter.PostAsJsonAsync(OrdersRoute, WaiterBody(
            Guid.NewGuid(),
            Line(ClassicCombo, 1, LargeSize, ColaDrink, [CheeseExtra, BaconExtra], []),
            Line(ClassicCombo, 2, LargeSize, ColaDrink, [BaconExtra, CheeseExtra], [])));

        // Assert
        Assert.Equal([OrdersErrors.DuplicatedProduct], await FieldMessagesAsync(response, "items"));
        Assert.Equal(0, await CountOrdersAsync());
    }

    [Fact]
    public async Task Post_ShouldAskForTheSize_WhenTheComboHasNoSize()
    {
        // Arrange (CA2, in the API)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);

        // Act
        using var response = await waiter.PostAsJsonAsync(OrdersRoute, WaiterBody(Guid.NewGuid(), Line(ClassicCombo, 1, null, ColaDrink, [], [])));

        // Assert
        Assert.Equal(["Selecciona el tamaño para continuar."], await FieldMessagesAsync(response, "items[0]"));
        Assert.Equal(0, await CountOrdersAsync());
    }

    [Fact]
    public async Task Post_ShouldAskForTheDrink_WhenTheComboHasNoDrink()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);

        // Act
        using var response = await waiter.PostAsJsonAsync(OrdersRoute, WaiterBody(Guid.NewGuid(), Line(ClassicCombo, 1, LargeSize, null, [], [])));

        // Assert
        Assert.Equal([OrdersErrors.DrinkRequiredMessage], await FieldMessagesAsync(response, "items[0]"));
    }

    [Fact]
    public async Task Post_ShouldRejectTheOrder_WhenAProductThatIsNotAComboCarriesOptions()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);

        // Act
        using var response = await waiter.PostAsJsonAsync(OrdersRoute, WaiterBody(Guid.NewGuid(), Line(ClassicBurger, 1, LargeSize, null, [], [])));

        // Assert
        Assert.Equal([OrdersErrors.NotCustomizableMessage], await FieldMessagesAsync(response, "items[0]"));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("size-is-a-drink")]
    [InlineData("extra-is-an-exclusion")]
    [InlineData("repeated-extra")]
    public async Task Post_ShouldRejectTheOrder_WhenAnOptionIsNotValidForItsPlace(string case_)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var line = case_ switch
        {
            "unknown" => Line(ClassicCombo, 1, Guid.NewGuid(), ColaDrink, [], []),
            "size-is-a-drink" => Line(ClassicCombo, 1, ColaDrink, ColaDrink, [], []),
            "extra-is-an-exclusion" => Line(ClassicCombo, 1, LargeSize, ColaDrink, [NoOnion], []),
            _ => Line(ClassicCombo, 1, LargeSize, ColaDrink, [CheeseExtra, CheeseExtra], []),
        };

        // Act
        using var response = await waiter.PostAsJsonAsync(OrdersRoute, WaiterBody(Guid.NewGuid(), line));

        // Assert
        var expected = case_ == "repeated-extra" ? OrdersErrors.DuplicatedOptionMessage : OrdersErrors.UnknownOptionMessage;
        Assert.Equal([expected], await FieldMessagesAsync(response, "items[0]"));
        Assert.Equal(0, await CountOrdersAsync());
    }

    [Fact]
    public async Task Get_ShouldShowTheCustomizationToTheKitchenAndInTheDetail_WhenAnOrderWithACustomizedComboWasSent()
    {
        // Arrange (CA3)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        using var created = await waiter.PostAsJsonAsync(OrdersRoute, WaiterBody(Guid.NewGuid(), Line(ClassicCombo, 1, LargeSize, ColaDrink, [CheeseExtra], [NoOnion])));
        var orderId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Act
        var queue = await kitchen.GetFromJsonAsync<JsonElement>("/api/v1/kitchen/queue");
        var detail = await waiter.GetFromJsonAsync<JsonElement>($"{OrdersRoute}/{orderId}");

        // Assert
        var kitchenItem = queue.GetProperty("orders").EnumerateArray().Single(order => order.GetProperty("id").GetGuid() == orderId).GetProperty("items")[0];
        var detailItem = detail.GetProperty("items")[0];
        foreach (var item in new[] { kitchenItem, detailItem })
        {
            Assert.Equal("Grande", item.GetProperty("size").GetString());
            Assert.Equal("Refresco de cola", item.GetProperty("drink").GetString());
            Assert.Equal(["Queso extra"], item.GetProperty("extras").EnumerateArray().Select(extra => extra.GetString()));
            Assert.Equal(["Cebolla"], item.GetProperty("exclusions").EnumerateArray().Select(exclusion => exclusion.GetString()));
        }
    }

    [Fact]
    public async Task Get_ShouldShowNoOptions_WhenTheProductIsNotACombo()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        using var created = await waiter.PostAsJsonAsync(OrdersRoute, WaiterBody(Guid.NewGuid(), Line(ClassicBurger, 1, null, null, [], [])));
        var orderId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Act
        var detail = await waiter.GetFromJsonAsync<JsonElement>($"{OrdersRoute}/{orderId}");

        // Assert
        var item = detail.GetProperty("items")[0];
        Assert.Equal(JsonValueKind.Null, item.GetProperty("size").ValueKind);
        Assert.Equal(JsonValueKind.Null, item.GetProperty("drink").ValueKind);
        Assert.Equal(0, item.GetProperty("extras").GetArrayLength());
        Assert.Equal(0, item.GetProperty("exclusions").GetArrayLength());
    }

    [Fact]
    public async Task Post_ShouldPriceAndSaveTheCustomization_WhenACustomerSendsAPickupOrder()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        var body = new
        {
            clientOrderId = Guid.NewGuid(),
            name = "Ana María",
            phone = "7711234567",
            items = new[] { Line(ClassicCombo, 1, LargeSize, ColaDrink, [BaconExtra], [NoTomato]) },
        };

        // Act
        using var response = await client.PostAsJsonAsync(PickupRoute, body);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(162m, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("total").GetDecimal()); // 119 + 25 + 18
        await using var connection = new SqlConnection(Database.ConnectionString);
        var saved = await connection.QuerySingleAsync<(string Size, string Drink)>("SELECT Size, Drink FROM dbo.OrderItems;");
        Assert.Equal(("Grande", "Refresco de cola"), saved);
    }

    [Fact]
    public async Task Post_ShouldAskForTheSize_WhenACustomerSendsAComboWithoutIt()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();
        var body = new
        {
            clientOrderId = Guid.NewGuid(),
            name = "Ana María",
            phone = "7711234567",
            items = new[] { Line(ClassicCombo, 1, null, null, [], []) },
        };

        // Act
        using var response = await client.PostAsJsonAsync(PickupRoute, body);

        // Assert
        Assert.Equal(["Selecciona el tamaño para continuar."], await FieldMessagesAsync(response, "items[0]"));
        Assert.Equal(0, await CountOrdersAsync());
    }

    private static object Line(Guid productId, int quantity, Guid? sizeId, Guid? drinkId, Guid[] extraIds, Guid[] exclusionIds) => new
    {
        productId,
        quantity,
        sizeId,
        drinkId,
        extraIds,
        exclusionIds,
    };

    private static object WaiterBody(Guid clientOrderId, params object[] items) => new { clientOrderId, tableId = TableFive, takeAway = false, items };

    private static async Task<(HttpClient Client, Guid EmployeeId)> SignInAsync(AuthApiFactory factory, EmployeeRole role)
    {
        const string Pin = "1234";
        var employee = await factory.AddEmployeeAsync($"Empleado {role} {Guid.NewGuid():N}", role, Pin);
        var client = factory.CreateClient();
        AuthApiFactory.Authorize(client, (await AuthApiFactory.LoginAsync(client, employee.Id, Pin)).AccessToken);
        return (client, employee.Id);
    }

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
