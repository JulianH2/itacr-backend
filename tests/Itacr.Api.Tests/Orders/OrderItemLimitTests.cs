using FluentValidation;
using Itacr.Api.Tests.Catalog;
using Itacr.Application.Catalog.GetProducts;
using Itacr.Application.Orders;
using Itacr.Application.Orders.CreateOrder;
using Itacr.Application.Orders.CreatePickupOrder;
using Itacr.Application.Orders.UpdateOrder;
using Itacr.Domain.Customers;
using Itacr.Domain.Orders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Itacr.Api.Tests.Orders;

/// <summary>
/// The limit of lines of a request (ITACR-66): the validator rejects a request with too many lines, and the use case refuses it as well,
/// so the size of a request never drives the memory it reserves nor the loops it walks.
/// </summary>
public sealed class OrderItemLimitTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 18, 0, 0, TimeSpan.Zero);
    private static readonly Guid Soda = Guid.Parse("20000000-0000-0000-0000-000000000012");

    [Fact]
    public async Task CreateOrderHandler_ShouldRefuseTheRequest_WhenItHasMoreLinesThanTheLimit()
    {
        // Arrange
        var handler = new CreateOrderHandler(
            new ForbiddenOrderRepository(),
            new ForbiddenDiningTableRepository(),
            new FakeCatalogRepository([], []),
            new FakeTimeProvider(Now),
            new AlwaysValidValidator<CreateOrderCommand>());

        // Act
        var result = await handler.HandleAsync(TooManyItems(), Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Equal(OrdersErrors.ItemsLimitExceeded, result.Error);
    }

    [Fact]
    public async Task UpdateOrderHandler_ShouldRefuseTheRequest_WhenItHasMoreLinesThanTheLimit()
    {
        // Arrange
        var handler = new UpdateOrderHandler(
            new ForbiddenOrderRepository(),
            new ForbiddenDiningTableRepository(),
            new FakeCatalogRepository([], []),
            new FakeTimeProvider(Now),
            new AlwaysValidValidator<UpdateOrderCommand>());

        // Act
        var result = await handler.HandleAsync(Guid.NewGuid(), TooManyItemsToUpdate(), Guid.NewGuid(), CancellationToken.None);
        // Assert
        Assert.Equal(OrdersErrors.ItemsLimitExceeded, result.Error);
    }

    [Fact]
    public async Task CreatePickupOrderHandler_ShouldRefuseTheRequest_WhenItHasMoreLinesThanTheLimit()
    {
        // Arrange
        var options = Options.Create(new PublicOrdersOptions { MaxProducts = 20 });
        var handler = new CreatePickupOrderHandler(
            new ForbiddenPickupOrderRepository(),
            new FakeCatalogRepository([], [new ProductResponse(Soda, Guid.NewGuid(), "Refresco de Cola", string.Empty, 29m, null, false)]),
            new FakeTimeProvider(Now),
            options,
            new AlwaysValidValidator<CreatePickupOrderCommand>());

        // Act
        var result = await handler.HandleAsync(
            new CreatePickupOrderCommand(Guid.NewGuid(), "Ana", "7711234567", TooManyItems().Items),
            CancellationToken.None);

        // Assert
        Assert.Equal(OrdersErrors.ItemsLimitExceeded, result.Error);
    }

    /// <summary>A request with one line more than the limit of the waiter.</summary>
    private static CreateOrderCommand TooManyItems() =>
        new(Guid.NewGuid(), null, true, [.. Enumerable.Range(0, OrderLimits.MaxItems + 1).Select(index => new CreateOrderItem(Guid.NewGuid(), 1))]);

    /// <summary>The same request, as the modification of an order.</summary>
    private static UpdateOrderCommand TooManyItemsToUpdate() =>
        new(null, [.. Enumerable.Range(0, OrderLimits.MaxItems + 1).Select(index => new CreateOrderItem(Guid.NewGuid(), 1))]);

    /// <summary>A validator without rules, so it accepts everything: the use case must not rely only on the validator.</summary>
    private sealed class AlwaysValidValidator<T> : AbstractValidator<T>;

    /// <summary>The repository of the waiter, which the use case must not reach when the request has too many lines.</summary>
    private sealed class ForbiddenOrderRepository : IOrderRepository
    {
        public Task<PlacedOrder?> GetByClientOrderIdAsync(Guid clientOrderId, CancellationToken cancellationToken) => throw Forbidden();

        public Task<bool> CreateAsync(Order order, CancellationToken cancellationToken) => throw Forbidden();

        public Task<OrderStatusState?> GetStatusAsync(Guid orderId, CancellationToken cancellationToken) => throw Forbidden();

        public Task<bool> ChangeStatusAsync(Guid orderId, OrderStatus from, OrderStatus to, Guid changedById, DateTime now, CancellationToken cancellationToken) => throw Forbidden();

        public Task<bool> UpdateAsync(OrderModification modification, CancellationToken cancellationToken) => throw Forbidden();

        public Task<OrderDetail?> GetDetailAsync(Guid orderId, CancellationToken cancellationToken) => throw Forbidden();

        public Task<IReadOnlyList<RecentOrder>> GetRecentByWaiterAsync(Guid waiterId, DateTime since, int take, CancellationToken cancellationToken) => throw Forbidden();

        public Task<IReadOnlyList<WaiterActiveOrder>> GetActiveByWaiterAsync(Guid waiterId, int take, CancellationToken cancellationToken) => throw Forbidden();

        public Task<IReadOnlyList<WaiterActiveOrder>> GetReadyPickupsAsync(int take, CancellationToken cancellationToken) => throw Forbidden();

        private static InvalidOperationException Forbidden() => new("The use case reached the database of a request it had to refuse.");
    }

    /// <summary>The tables of the restaurant, which the use case must not reach when the request has too many lines.</summary>
    private sealed class ForbiddenDiningTableRepository : IDiningTableRepository
    {
        public Task<IReadOnlyList<DiningTable>> GetAllAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The use case reached the tables of a request it had to refuse.");
    }

    /// <summary>The orders of the customers, which the use case must not reach when the request has too many lines.</summary>
    private sealed class ForbiddenPickupOrderRepository : IPickupOrderRepository
    {
        public Task<PickupOrder?> GetByClientOrderIdAsync(Guid clientOrderId, CancellationToken cancellationToken) => throw Forbidden();

        public Task<PickupCreateOutcome> CreateAsync(Order order, Customer customer, int maxActiveOrders, CancellationToken cancellationToken) => throw Forbidden();

        private static InvalidOperationException Forbidden() => new("The use case reached the database of a request it had to refuse.");
    }
}
