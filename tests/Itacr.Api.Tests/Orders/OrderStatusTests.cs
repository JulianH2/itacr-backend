using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Itacr.Api.Tests.Auth;
using Itacr.Application.Orders;
using Itacr.Domain.Identity;
using Itacr.Domain.Orders;
using Microsoft.Data.SqlClient;

namespace Itacr.Api.Tests.Orders;

/// <summary>HU_05: the kitchen moves an order to its next status, and can undo an accidental tap. Runs against the real procedures.</summary>
public sealed class OrderStatusTests(DatabaseFixture database) : DatabaseTest(database)
{
    private static readonly Guid TableFive = Guid.Parse("40000000-0000-0000-0000-000000000005");
    private static readonly Guid Soda = Guid.Parse("20000000-0000-0000-0000-000000000012");

    [Fact]
    public async Task Post_ShouldMoveANewOrderToInPreparationAndRecordTheTime_WhenTheKitchenTapsIt()
    {
        // Arrange (CA1)
        await using var factory = new AuthApiFactory(Database);
        var orderId = await PlaceOrderAsync(factory);
        var (kitchen, cookId) = await SignInAsync(factory, EmployeeRole.Kitchen);
        factory.Time.Advance(TimeSpan.FromMinutes(2));
        var changedAt = factory.Time.GetUtcNow().UtcDateTime;

        // Act
        using var response = await kitchen.PostAsJsonAsync(StatusRoute(orderId), new { status = "InPreparation" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("InPreparation", body.GetProperty("status").GetString());
        var saved = await GetOrderAsync(orderId);
        Assert.Equal("InPreparation", saved.Status);
        Assert.Equal(changedAt, saved.LastStatusChangeAt, TimeSpan.FromMilliseconds(1));
        Assert.Null(saved.ReadyAt);
        var last = (await GetHistoryAsync(orderId))[^1];
        Assert.Equal(("New", "InPreparation", cookId), (last.FromStatus, last.ToStatus, last.ChangedById));
    }

    [Fact]
    public async Task Post_ShouldMoveAnOrderInPreparationToReadyAndRecordWhenItWasReady_WhenTheKitchenTapsIt()
    {
        // Arrange (CA2)
        await using var factory = new AuthApiFactory(Database);
        var orderId = await PlaceOrderAsync(factory);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        (await kitchen.PostAsJsonAsync(StatusRoute(orderId), new { status = "InPreparation" })).EnsureSuccessStatusCode();
        factory.Time.Advance(TimeSpan.FromMinutes(7));
        var readyAt = factory.Time.GetUtcNow().UtcDateTime;

        // Act
        using var response = await kitchen.PostAsJsonAsync(StatusRoute(orderId), new { status = "Ready" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await GetOrderAsync(orderId);
        Assert.Equal("Ready", saved.Status);
        Assert.NotNull(saved.ReadyAt);
        Assert.Equal(readyAt, saved.ReadyAt.Value, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task Post_ShouldAnswerConflictAndChangeNothing_WhenTheOrderSkipsAStep()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var orderId = await PlaceOrderAsync(factory);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);

        // Act
        using var response = await kitchen.PostAsJsonAsync(StatusRoute(orderId), new { status = "Ready" });

        // Assert
        await AssertConflictAsync(response, OrdersErrors.StatusStepNotAllowed.Detail);
        Assert.Equal("New", (await GetOrderAsync(orderId)).Status);
    }

    [Fact]
    public async Task Post_ShouldAnswerConflict_WhenAnotherBoardAlreadyMovedTheOrder()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var orderId = await PlaceOrderAsync(factory);
        var (firstBoard, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var (secondBoard, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        (await firstBoard.PostAsJsonAsync(StatusRoute(orderId), new { status = "InPreparation" })).EnsureSuccessStatusCode();

        // Act
        using var response = await secondBoard.PostAsJsonAsync(StatusRoute(orderId), new { status = "InPreparation" });

        // Assert
        await AssertConflictAsync(response, OrdersErrors.StatusAlreadyChanged.Detail);
        Assert.Equal(2, (await GetHistoryAsync(orderId)).Count);
    }

    [Fact]
    public async Task Post_ShouldRejectDelivered_WhenTheKitchenTriesToMarkItDelivered()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var orderId = await PlaceOrderAsync(factory);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);

        // Act
        using var response = await kitchen.PostAsJsonAsync(StatusRoute(orderId), new { status = "Delivered" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal([OrdersErrors.KitchenStatusOnly], problem.GetProperty("errors").GetProperty("status").EnumerateArray().Select(message => message.GetString()));
    }

    [Fact]
    public async Task Post_ShouldAnswerNotFound_WhenTheOrderDoesNotExist()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);

        // Act
        using var response = await kitchen.PostAsJsonAsync(StatusRoute(Guid.NewGuid()), new { status = "InPreparation" });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_ShouldBeForbidden_WhenAWaiterTriesToChangeTheStatus()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var orderId = await PlaceOrderAsync(factory);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);

        // Act
        using var response = await waiter.PostAsJsonAsync(StatusRoute(orderId), new { status = "InPreparation" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Undo_ShouldTakeTheOrderBackAndClearReadyAt_WhenTheCookUndoesWithinTheWindow()
    {
        // Arrange (CA3)
        await using var factory = new AuthApiFactory(Database);
        var orderId = await PlaceOrderAsync(factory);
        var (kitchen, cookId) = await SignInAsync(factory, EmployeeRole.Kitchen);
        (await kitchen.PostAsJsonAsync(StatusRoute(orderId), new { status = "InPreparation" })).EnsureSuccessStatusCode();
        (await kitchen.PostAsJsonAsync(StatusRoute(orderId), new { status = "Ready" })).EnsureSuccessStatusCode();
        factory.Time.Advance(TimeSpan.FromSeconds(5));

        // Act
        using var response = await kitchen.PostAsync(UndoRoute(orderId), content: null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("InPreparation", body.GetProperty("status").GetString());
        var saved = await GetOrderAsync(orderId);
        Assert.Equal("InPreparation", saved.Status);
        Assert.Null(saved.ReadyAt);
        var last = (await GetHistoryAsync(orderId))[^1];
        Assert.Equal(("Ready", "InPreparation", cookId), (last.FromStatus, last.ToStatus, last.ChangedById));
    }

    [Fact]
    public async Task Undo_ShouldAnswerConflict_WhenTheWindowHasPassed()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var orderId = await PlaceOrderAsync(factory);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        (await kitchen.PostAsJsonAsync(StatusRoute(orderId), new { status = "InPreparation" })).EnsureSuccessStatusCode();
        factory.Time.Advance(OrderStatusRules.UndoWindow + TimeSpan.FromSeconds(1));

        // Act
        using var response = await kitchen.PostAsync(UndoRoute(orderId), content: null);

        // Assert
        await AssertConflictAsync(response, OrdersErrors.NothingToUndo.Detail);
        Assert.Equal("InPreparation", (await GetOrderAsync(orderId)).Status);
    }

    [Fact]
    public async Task Undo_ShouldAnswerConflict_WhenAnotherCookMadeTheChange()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var orderId = await PlaceOrderAsync(factory);
        var (cook, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var (otherCook, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        (await cook.PostAsJsonAsync(StatusRoute(orderId), new { status = "InPreparation" })).EnsureSuccessStatusCode();

        // Act
        using var response = await otherCook.PostAsync(UndoRoute(orderId), content: null);

        // Assert
        await AssertConflictAsync(response, OrdersErrors.UndoByAnotherEmployee.Detail);
    }

    [Fact]
    public async Task Undo_ShouldAnswerConflict_WhenTheLastChangeWasAlreadyUndone()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var orderId = await PlaceOrderAsync(factory);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        (await kitchen.PostAsJsonAsync(StatusRoute(orderId), new { status = "InPreparation" })).EnsureSuccessStatusCode();
        (await kitchen.PostAsync(UndoRoute(orderId), content: null)).EnsureSuccessStatusCode();

        // Act
        using var response = await kitchen.PostAsync(UndoRoute(orderId), content: null);

        // Assert
        await AssertConflictAsync(response, OrdersErrors.NothingToUndo.Detail);
        Assert.Equal("New", (await GetOrderAsync(orderId)).Status);
    }

    private static string StatusRoute(Guid orderId) => $"/api/v1/orders/{orderId}/status";

    private static string UndoRoute(Guid orderId) => $"/api/v1/orders/{orderId}/status/undo";

    private static async Task<(HttpClient Client, Guid EmployeeId)> SignInAsync(AuthApiFactory factory, EmployeeRole role)
    {
        const string Pin = "1234";
        var employee = await factory.AddEmployeeAsync($"Empleado {role}", role, Pin);
        var client = factory.CreateClient();
        AuthApiFactory.Authorize(client, (await factory.LoginAsync(client, employee.Id, Pin)).AccessToken);
        return (client, employee.Id);
    }

    private static async Task<Guid> PlaceOrderAsync(AuthApiFactory factory)
    {
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        using var response = await waiter.PostAsJsonAsync("/api/v1/orders", new
        {
            clientOrderId = Guid.NewGuid(),
            tableId = TableFive,
            takeAway = false,
            items = new[] { new { productId = Soda, quantity = 1 } },
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task AssertConflictAsync(HttpResponseMessage response, string detail)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(detail, problem.GetProperty("detail").GetString());
        Assert.Equal(OrdersErrors.StatusConflictType, problem.GetProperty("type").GetString());
    }

    private async Task<(string Status, DateTime LastStatusChangeAt, DateTime? ReadyAt)> GetOrderAsync(Guid orderId)
    {
        await using var connection = new SqlConnection(Database.ConnectionString);
        return await connection.QuerySingleAsync<(string, DateTime, DateTime?)>(
            "SELECT Status, LastStatusChangeAt, ReadyAt FROM dbo.Orders WHERE Id = @Id;", new { Id = orderId });
    }

    private async Task<List<(string? FromStatus, string ToStatus, Guid? ChangedById)>> GetHistoryAsync(Guid orderId)
    {
        await using var connection = new SqlConnection(Database.ConnectionString);
        var rows = await connection.QueryAsync<(string?, string, Guid?)>(
            "SELECT FromStatus, ToStatus, ChangedById FROM dbo.OrderStatusHistory WHERE OrderId = @Id ORDER BY CreatedAt, Id;", new { Id = orderId });
        return [.. rows];
    }
}
