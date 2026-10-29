using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Itacr.Api.Tests.Auth;
using Itacr.Application.Notifications;
using Itacr.Domain.Identity;
using Microsoft.Data.SqlClient;

namespace Itacr.Api.Tests.Notifications;

/// <summary>HU_09: the waiter or the customer gets a push notice when the kitchen marks their order as Ready.</summary>
public sealed class PushNotificationTests(DatabaseFixture database) : DatabaseTest(database)
{
    private const string SubscriptionsRoute = "/api/v1/push/subscriptions";
    private const string PublicKeyRoute = "/api/v1/public/push/public-key";

    private static readonly Guid TableThree = Guid.Parse("40000000-0000-0000-0000-000000000003");
    private static readonly Guid Soda = Guid.Parse("20000000-0000-0000-0000-000000000012");

    [Fact]
    public async Task Ready_ShouldNotifyTheWaiterWithTheOrderNumberAndTable_WhenTheKitchenMarksTheirOrderReady()
    {
        // Arrange (CA1, CA3)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, waiterId) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var endpoint = await SubscribeAsync(waiter);
        var orderId = await PlaceOrderAsync(waiter, TableThree);

        // Act
        await MoveToReadyAsync(kitchen, orderId);
        var sent = await factory.PushSender.WaitForAsync(1);

        // Assert
        var (subscription, message) = Assert.Single(sent);
        Assert.Equal(endpoint, subscription.Endpoint);
        Assert.Equal("Pedido listo", message.Title);
        Assert.Matches(@"^Orden #\d+ · Mesa 3 está lista para entregar\.$", message.Body);
        Assert.Equal($"/orders/detail/{orderId}", message.Url);

        await using var connection = new SqlConnection(Database.ConnectionString);
        var notification = await connection.QuerySingleAsync<(Guid Id, Guid? RecipientEmployeeId, DateTime SentAt, DateTime? ReadAt)>(
            "SELECT Id, RecipientEmployeeId, SentAt, ReadAt FROM dbo.Notifications WHERE OrderId = @OrderId AND Type = N'OrderReady';",
            new { OrderId = orderId });
        Assert.Equal(message.NotificationId, notification.Id);
        Assert.Equal(waiterId, notification.RecipientEmployeeId);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public async Task Ready_ShouldNotifyTheCustomerWithoutPersonalData_WhenTheirPickupOrderIsReady()
    {
        // Arrange (CA2)
        await using var factory = new AuthApiFactory(Database);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        using var customer = factory.CreateClient();
        var clientOrderId = Guid.NewGuid();
        (await customer.PostAsJsonAsync("/api/v1/public/orders", new
        {
            clientOrderId,
            name = "Ana María",
            phone = "7719876543",
            items = new[] { new { productId = Soda, quantity = 1 } },
        })).EnsureSuccessStatusCode();
        var endpoint = NewEndpoint();
        using var subscribed = await customer.PostAsJsonAsync($"/api/v1/public/orders/{clientOrderId}/push-subscription", Subscription(endpoint));

        // Act
        await MoveToReadyAsync(kitchen, await OrderIdAsync(clientOrderId));
        var sent = await factory.PushSender.WaitForAsync(1);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, subscribed.StatusCode);
        var (subscription, message) = Assert.Single(sent);
        Assert.Equal(endpoint, subscription.Endpoint);
        Assert.Matches(@"^Tu pedido #\d+ está listo\. Ya puedes pasar por él\.$", message.Body);
        Assert.Equal("/pickup/orders", message.Url);
        Assert.DoesNotContain("Ana", message.Body + message.Title);
        Assert.DoesNotContain("7719876543", message.Body + message.Title);
    }

    [Fact]
    public async Task Ready_ShouldSendOneNotice_WhenTheKitchenUndoesReadyAndMarksItAgain()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        await SubscribeAsync(waiter);
        var orderId = await PlaceOrderAsync(waiter, TableThree);
        await MoveToReadyAsync(kitchen, orderId);
        await factory.PushSender.WaitForAsync(1);

        // Act
        (await kitchen.PostAsync($"/api/v1/orders/{orderId}/status/undo", content: null)).EnsureSuccessStatusCode();
        (await kitchen.PostAsJsonAsync($"/api/v1/orders/{orderId}/status", new { status = "Ready" })).EnsureSuccessStatusCode();
        await FakePushSender.SettleAsync();

        // Assert
        Assert.Single(factory.PushSender.Sent);
        await using var connection = new SqlConnection(Database.ConnectionString);
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.Notifications WHERE OrderId = @OrderId AND Type = N'OrderReady';", new { OrderId = orderId }));
    }

    [Fact]
    public async Task Ready_ShouldStillMarkTheOrderReadyAndSendNothing_WhenNobodyAcceptedNotifications()
    {
        // Arrange (CA4)
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var orderId = await PlaceOrderAsync(waiter, TableThree);

        // Act
        await MoveToReadyAsync(kitchen, orderId);
        await FakePushSender.SettleAsync();
        var order = await waiter.GetFromJsonAsync<JsonElement>($"/api/v1/orders/{orderId}");

        // Assert
        Assert.Empty(factory.PushSender.Sent);
        Assert.Equal("Ready", order.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Ready_ShouldOnlyNotifyTheWaiterWhoTookTheOrder_WhenTwoWaitersAreSubscribed()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (otherWaiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var mine = await SubscribeAsync(waiter);
        await SubscribeAsync(otherWaiter);
        var orderId = await PlaceOrderAsync(waiter, TableThree);

        // Act
        await MoveToReadyAsync(kitchen, orderId);
        await factory.PushSender.WaitForAsync(1);
        await FakePushSender.SettleAsync();

        // Assert
        Assert.Equal([mine], factory.PushSender.Sent.Select(sent => sent.Subscription.Endpoint));
    }

    [Fact]
    public async Task Ready_ShouldDeleteTheSubscription_WhenThePushServiceSaysItExpired()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        var endpoint = await SubscribeAsync(waiter);
        factory.PushSender.GoneEndpoints[endpoint] = true;
        var orderId = await PlaceOrderAsync(waiter, TableThree);

        // Act
        await MoveToReadyAsync(kitchen, orderId);
        await factory.PushSender.WaitForAsync(1);
        await FakePushSender.SettleAsync();

        // Assert
        await using var connection = new SqlConnection(Database.ConnectionString);
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.PushSubscriptions WHERE Endpoint = @Endpoint;", new { Endpoint = endpoint }));
    }

    [Fact]
    public async Task Subscribe_ShouldKeepOneRowAndMoveItToTheLastWaiter_WhenTheSameBrowserIsSavedTwice()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (first, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (second, secondId) = await SignInAsync(factory, EmployeeRole.Waiter);
        var endpoint = await SubscribeAsync(first);

        // Act
        using var response = await second.PostAsJsonAsync(SubscriptionsRoute, Subscription(endpoint));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var connection = new SqlConnection(Database.ConnectionString);
        var owner = await connection.QuerySingleAsync<Guid?>("SELECT EmployeeId FROM dbo.PushSubscriptions WHERE Endpoint = @Endpoint;", new { Endpoint = endpoint });
        Assert.Equal(secondId, owner);
    }

    [Theory]
    [InlineData("http://push.example.com/abc", "BKey", "auth")]
    [InlineData("https://push.example.com/abc", "", "auth")]
    [InlineData("https://push.example.com/abc", "BKey", "no spaces allowed")]
    public async Task Subscribe_ShouldReject_WhenTheSubscriptionIsNotValid(string endpoint, string p256dh, string auth)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);

        // Act
        using var response = await waiter.PostAsJsonAsync(SubscriptionsRoute, new { endpoint, p256dh, auth });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(NotificationsErrors.SubscriptionInvalid, problem.GetProperty("errors").EnumerateObject().SelectMany(field => field.Value.EnumerateArray()).Select(message => message.GetString()));
    }

    [Theory]
    [InlineData(EmployeeRole.Kitchen)]
    [InlineData(EmployeeRole.Manager)]
    public async Task Subscribe_ShouldBeForbidden_WhenTheEmployeeIsNotAWaiter(EmployeeRole role)
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (client, _) = await SignInAsync(factory, role);

        // Act
        using var response = await client.PostAsJsonAsync(SubscriptionsRoute, Subscription(NewEndpoint()));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SubscribePickup_ShouldAnswerNotFound_WhenTheIdIsAWaiterOrderOrDoesNotExist()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var waiterClientOrderId = Guid.NewGuid();
        (await waiter.PostAsJsonAsync("/api/v1/orders", new
        {
            clientOrderId = waiterClientOrderId,
            tableId = TableThree,
            takeAway = false,
            items = new[] { new { productId = Soda, quantity = 1 } },
        })).EnsureSuccessStatusCode();
        using var anonymous = factory.CreateClient();

        // Act
        using var waiterOrder = await anonymous.PostAsJsonAsync($"/api/v1/public/orders/{waiterClientOrderId}/push-subscription", Subscription(NewEndpoint()));
        using var missing = await anonymous.PostAsJsonAsync($"/api/v1/public/orders/{Guid.NewGuid()}/push-subscription", Subscription(NewEndpoint()));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, waiterOrder.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task PublicKey_ShouldReturnTheConfiguredKey_WhenPushIsConfigured()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database, new Dictionary<string, string>
        {
            ["WebPush:PublicKey"] = "BPublicKeyForTests",
            ["WebPush:PrivateKey"] = "private-key-for-tests",
        });
        using var client = factory.CreateClient();

        // Act
        var answer = await client.GetFromJsonAsync<JsonElement>(PublicKeyRoute);

        // Assert
        Assert.Equal("BPublicKeyForTests", answer.GetProperty("publicKey").GetString());
        Assert.False(answer.TryGetProperty("privateKey", out _));
    }

    [Fact]
    public async Task PublicKey_ShouldAnswerNotFound_WhenPushIsNotConfigured()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(PublicKeyRoute);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkRead_ShouldRecordTheFirstOpening_WhenTheRecipientOpensTheNotification()
    {
        // Arrange
        await using var factory = new AuthApiFactory(Database);
        var (waiter, _) = await SignInAsync(factory, EmployeeRole.Waiter);
        var (kitchen, _) = await SignInAsync(factory, EmployeeRole.Kitchen);
        await SubscribeAsync(waiter);
        var orderId = await PlaceOrderAsync(waiter, TableThree);
        await MoveToReadyAsync(kitchen, orderId);
        var notificationId = (await factory.PushSender.WaitForAsync(1))[0].Message.NotificationId;
        using var anonymous = factory.CreateClient();

        // Act
        using var first = await anonymous.PostAsync($"/api/v1/public/notifications/{notificationId}/read", content: null);
        await using var connection = new SqlConnection(Database.ConnectionString);
        var readAt = await connection.QuerySingleAsync<DateTime?>("SELECT ReadAt FROM dbo.Notifications WHERE Id = @Id;", new { Id = notificationId });
        factory.Time.Advance(TimeSpan.FromMinutes(5));
        using var second = await anonymous.PostAsync($"/api/v1/public/notifications/{notificationId}/read", content: null);
        using var unknown = await anonymous.PostAsync($"/api/v1/public/notifications/{Guid.NewGuid()}/read", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.NotNull(readAt);
        Assert.Equal(readAt, await connection.QuerySingleAsync<DateTime?>("SELECT ReadAt FROM dbo.Notifications WHERE Id = @Id;", new { Id = notificationId }));
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, unknown.StatusCode);
    }

    private static string NewEndpoint() => $"https://push.example.com/send/{Guid.NewGuid():N}";

    private static object Subscription(string endpoint) => new
    {
        endpoint,
        p256dh = "BNcRdreALRFXTkOOUHK1EtK2wtaz5Ry4YfYCA_0QTpQtUbVlUls0VJXg7A8u-Ts1XbjhazAkj7I99e8QcYP7DkM",
        auth = "tBHItJI5svbpez7KI4CCXg",
    };

    private static async Task<string> SubscribeAsync(HttpClient waiter)
    {
        var endpoint = NewEndpoint();
        (await waiter.PostAsJsonAsync(SubscriptionsRoute, Subscription(endpoint))).EnsureSuccessStatusCode();
        return endpoint;
    }

    private static async Task MoveToReadyAsync(HttpClient kitchen, Guid orderId)
    {
        (await kitchen.PostAsJsonAsync($"/api/v1/orders/{orderId}/status", new { status = "InPreparation" })).EnsureSuccessStatusCode();
        (await kitchen.PostAsJsonAsync($"/api/v1/orders/{orderId}/status", new { status = "Ready" })).EnsureSuccessStatusCode();
    }

    private static async Task<Guid> PlaceOrderAsync(HttpClient waiter, Guid tableId)
    {
        using var response = await waiter.PostAsJsonAsync("/api/v1/orders", new
        {
            clientOrderId = Guid.NewGuid(),
            tableId,
            takeAway = false,
            items = new[] { new { productId = Soda, quantity = 1 } },
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<(HttpClient Client, Guid EmployeeId)> SignInAsync(AuthApiFactory factory, EmployeeRole role)
    {
        const string Pin = "1234";
        var employee = await factory.AddEmployeeAsync($"Empleado {role} {Guid.NewGuid():N}", role, Pin);
        var client = factory.CreateClient();
        AuthApiFactory.Authorize(client, (await factory.LoginAsync(client, employee.Id, Pin)).AccessToken);
        return (client, employee.Id);
    }

    private async Task<Guid> OrderIdAsync(Guid clientOrderId)
    {
        await using var connection = new SqlConnection(Database.ConnectionString);
        return await connection.QuerySingleAsync<Guid>("SELECT Id FROM dbo.Orders WHERE ClientOrderId = @ClientOrderId;", new { ClientOrderId = clientOrderId });
    }
}
