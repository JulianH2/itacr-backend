using Itacr.Application.Notifications;
using Itacr.Application.Orders;
using Itacr.Application.Orders.ChangeOrderStatus;
using Itacr.Application.Orders.CreateOrder;
using Itacr.Application.Orders.DeliverOrder;
using Itacr.Application.Orders.UndoOrderStatusChange;
using Itacr.Application.Orders.UpdateOrder;
using Itacr.Domain.Orders;
using Microsoft.Extensions.Time.Testing;

namespace Itacr.Api.Tests.Orders;

/// <summary>
/// What the use cases answer when two people change the same order at the same time (ITACR-66): the second one gets a conflict, and
/// nothing is changed twice.
/// </summary>
public sealed class ConcurrentOrderChangesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 18, 0, 0, TimeSpan.Zero);
    private static readonly Guid Soda = Guid.Parse("20000000-0000-0000-0000-000000000012");
    private static readonly Guid Waiter = Guid.Parse("a1f0c6d2-0000-4000-8000-000000000001");
    private static readonly Guid Kitchen = Guid.Parse("a1f0c6d2-0000-4000-8000-000000000002");
    private static readonly Guid OrderId = Guid.Parse("b2f0c6d2-0000-4000-8000-000000000001");

    [Fact]
    public async Task ChangeOrderStatusHandler_ShouldAnswerAConflict_WhenAnotherBoardMovedTheOrderFirst()
    {
        // Arrange
        var repository = new StubOrderRepository
        {
            Status = new OrderStatusState(OrderId, OrderStatus.New, null, new OrderStatusChange(null, OrderStatus.New, null, Now.UtcDateTime)),
            ChangeStatusResult = false,
        };
        var handler = new ChangeOrderStatusHandler(repository, new FakeTimeProvider(Now), new StubOrderReadyQueue(), new ChangeOrderStatusValidator());

        // Act
        var result = await handler.HandleAsync(OrderId, new ChangeOrderStatusCommand(OrderStatus.InPreparation), Kitchen, CancellationToken.None);

        // Assert
        Assert.Equal(OrdersErrors.StatusAlreadyChanged, result.Error);
    }

    [Fact]
    public async Task DeliverOrderHandler_ShouldAnswerAConflict_WhenAnotherWaiterDeliveredTheOrderFirst()
    {
        // Arrange
        var repository = new StubOrderRepository
        {
            Status = new OrderStatusState(OrderId, OrderStatus.Ready, Waiter, new OrderStatusChange(OrderStatus.InPreparation, OrderStatus.Ready, Kitchen, Now.UtcDateTime)),
            ChangeStatusResult = false,
        };
        var handler = new DeliverOrderHandler(repository, new FakeTimeProvider(Now));

        // Act
        var result = await handler.HandleAsync(OrderId, Waiter, CancellationToken.None);

        // Assert
        Assert.Equal(OrdersErrors.StatusAlreadyChanged, result.Error);
    }

    [Fact]
    public async Task UndoOrderStatusChangeHandler_ShouldAnswerNotFound_WhenTheOrderDoesNotExist()
    {
        // Arrange
        var handler = new UndoOrderStatusChangeHandler(new StubOrderRepository(), new FakeTimeProvider(Now));

        // Act
        var result = await handler.HandleAsync(OrderId, Kitchen, CancellationToken.None);

        // Assert
        Assert.Equal(OrdersErrors.OrderNotFound, result.Error);
    }

    [Fact]
    public async Task UndoOrderStatusChangeHandler_ShouldAnswerAConflict_WhenAnotherCookUndidTheStepFirst()
    {
        // Arrange
        var repository = new StubOrderRepository
        {
            Status = new OrderStatusState(OrderId, OrderStatus.InPreparation, null, new OrderStatusChange(OrderStatus.New, OrderStatus.InPreparation, Kitchen, Now.UtcDateTime)),
            ChangeStatusResult = false,
        };
        var handler = new UndoOrderStatusChangeHandler(repository, new FakeTimeProvider(Now));

        // Act
        var result = await handler.HandleAsync(OrderId, Kitchen, CancellationToken.None);

        // Assert
        Assert.Equal(OrdersErrors.NothingToUndo, result.Error);
    }

    [Fact]
    public async Task UpdateOrderHandler_ShouldAnswerAConflict_WhenTheOrderChangedWhileTheRequestWasInProgress()
    {
        // Arrange
        var repository = new StubOrderRepository
        {
            Detail = Detail(),
            UpdateResult = false,
        };
        var handler = new UpdateOrderHandler(
            repository,
            new StubDiningTableRepository(),
            new Itacr.Api.Tests.Catalog.FakeCatalogRepository([], [new Itacr.Application.Catalog.GetProducts.ProductResponse(Soda, Guid.NewGuid(), "Refresco de Cola", string.Empty, 29m, null, false)]),
            new FakeTimeProvider(Now),
            new UpdateOrderValidator());

        // Act
        var result = await handler.HandleAsync(OrderId, new UpdateOrderCommand(null, [new CreateOrderItem(Soda, 1)]), Waiter, CancellationToken.None);

        // Assert
        Assert.Equal(OrdersErrors.OrderChangedMeanwhile, result.Error);
    }

    private static OrderDetail Detail() => new(
        OrderId,
        7,
        OrderChannel.TakeAway,
        OrderStatus.New,
        null,
        Waiter,
        29m,
        Now.UtcDateTime,
        Now.UtcDateTime,
        null,
        null,
        null,
        [new OrderDetailItem("Refresco de Cola", 1, 29m, 29m, null, null, [], [], Soda, null, null, [], [])],
        [],
        [new OrderStatusChange(null, OrderStatus.New, Waiter, Now.UtcDateTime)]);

    /// <summary>The tables of the restaurant, none of them needed by these tests.</summary>
    private sealed class StubDiningTableRepository : IDiningTableRepository
    {
        public Task<IReadOnlyList<DiningTable>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DiningTable>>([]);
    }

    /// <summary>The queue of the notices of the orders that are ready.</summary>
    private sealed class StubOrderReadyQueue : IOrderReadyQueue
    {
        public void Enqueue(Guid orderId)
        {
        }
    }
}
