using Itacr.Application.Kitchen;
using Itacr.Application.Kitchen.GetKitchenQueue;
using Itacr.Domain.Orders;
using Microsoft.Extensions.Time.Testing;

namespace Itacr.Api.Tests.Kitchen;

public sealed class KitchenQueueHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_ShouldOrderEveryChannelByCreationTime_WhenTheRepositoryReturnsThemMixed()
    {
        // Arrange
        var pickup = Order(1, OrderChannel.MobileApp, minutesAgo: 3);
        var tableOrder = Order(2, OrderChannel.DineIn, minutesAgo: 8, tableNumber: 5);
        var takeAway = Order(3, OrderChannel.TakeAway, minutesAgo: 5);
        var syncedLate = Order(4, OrderChannel.DineIn, minutesAgo: 20, tableNumber: 2);
        var handler = new GetKitchenQueueHandler(new StubKitchenRepository([pickup, tableOrder, takeAway, syncedLate]), new FakeTimeProvider(Now));

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.Equal([4, 2, 3, 1], result.Value.Orders.Select(order => order.OrderNumber));
    }

    [Fact]
    public async Task HandleAsync_ShouldBreakTiesWithTheOrderNumber_WhenTwoOrdersShareTheCreationTime()
    {
        // Arrange
        var second = Order(12, OrderChannel.DineIn, minutesAgo: 1, tableNumber: 1);
        var first = Order(11, OrderChannel.TakeAway, minutesAgo: 1);
        var handler = new GetKitchenQueueHandler(new StubKitchenRepository([second, first]), new FakeTimeProvider(Now));

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.Equal([11, 12], result.Value.Orders.Select(order => order.OrderNumber));
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTheServerTimeAndTheItemsOfEachOrder_WhenThereAreOrders()
    {
        // Arrange
        var order = Order(7, OrderChannel.DineIn, minutesAgo: 2, tableNumber: 4) with
        {
            Items = [new ActiveOrderItem("Combo Clásico", 2, "Grande", "Refresco de Cola", ["Queso extra"], ["Cebolla"])],
        };
        var handler = new GetKitchenQueueHandler(new StubKitchenRepository([order]), new FakeTimeProvider(Now));

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.Equal(Now.UtcDateTime, result.Value.ServerTime);
        var item = Assert.Single(Assert.Single(result.Value.Orders).Items);
        Assert.Equal(("Combo Clásico", 2, "Grande", "Refresco de Cola"), (item.ProductName, item.Quantity, item.Size, item.Drink));
        Assert.Equal(["Queso extra"], item.Extras);
        Assert.Equal(["Cebolla"], item.Exclusions);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNoOrders_WhenNothingIsActive()
    {
        // Arrange
        var handler = new GetKitchenQueueHandler(new StubKitchenRepository([]), new FakeTimeProvider(Now));

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Orders);
    }

    private static ActiveOrder Order(int number, OrderChannel channel, int minutesAgo, int? tableNumber = null) => new(
        Guid.NewGuid(),
        number,
        channel,
        OrderStatus.New,
        tableNumber,
        Now.UtcDateTime.AddMinutes(-minutesAgo),
        []);

    private sealed class StubKitchenRepository(IReadOnlyList<ActiveOrder> orders) : IKitchenRepository
    {
        public Task<IReadOnlyList<ActiveOrder>> GetActiveOrdersAsync(CancellationToken cancellationToken) => Task.FromResult(orders);
    }
}
