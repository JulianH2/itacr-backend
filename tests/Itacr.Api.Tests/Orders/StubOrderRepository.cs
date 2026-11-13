using Itacr.Application.Orders;
using Itacr.Domain.Orders;

namespace Itacr.Api.Tests.Orders;

/// <summary>
/// The repository of the orders, with the answers the test needs (ITACR-66): it lets a use case be checked without the database, and
/// without waiting for a race that is hard to provoke.
/// </summary>
internal sealed class StubOrderRepository : IOrderRepository
{
    /// <summary>The status of the order, with its last change. Null when the order does not exist.</summary>
    public OrderStatusState? Status { get; set; }

    /// <summary>The detail of the order, with the waiter that took it. Null when the order does not exist.</summary>
    public OrderDetail? Detail { get; set; }

    /// <summary>The order already saved with the same client order id, when the request is a retry.</summary>
    public PlacedOrder? Existing { get; set; }

    /// <summary>What the change of the status answers: false when another board moved the order first.</summary>
    public bool ChangeStatusResult { get; set; } = true;

    /// <summary>What the modification of the order answers: false when the order changed while the request was in progress.</summary>
    public bool UpdateResult { get; set; } = true;

    /// <summary>What the save of the order answers: false when the order was already saved.</summary>
    public bool CreateResult { get; set; } = true;

    public Task<PlacedOrder?> GetByClientOrderIdAsync(Guid clientOrderId, CancellationToken cancellationToken) => Task.FromResult(Existing);

    public Task<bool> CreateAsync(Order order, CancellationToken cancellationToken) => Task.FromResult(CreateResult);

    public Task<OrderStatusState?> GetStatusAsync(Guid orderId, CancellationToken cancellationToken) => Task.FromResult(Status);

    public Task<bool> ChangeStatusAsync(Guid orderId, OrderStatus from, OrderStatus to, Guid changedById, DateTime now, CancellationToken cancellationToken) =>
        Task.FromResult(ChangeStatusResult);

    public Task<bool> UpdateAsync(OrderModification modification, CancellationToken cancellationToken) => Task.FromResult(UpdateResult);

    public Task<OrderDetail?> GetDetailAsync(Guid orderId, CancellationToken cancellationToken) => Task.FromResult(Detail);

    public Task<IReadOnlyList<RecentOrder>> GetRecentByWaiterAsync(Guid waiterId, DateTime since, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RecentOrder>>([]);

    public Task<IReadOnlyList<WaiterActiveOrder>> GetActiveByWaiterAsync(Guid waiterId, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<WaiterActiveOrder>>([]);

    public Task<IReadOnlyList<WaiterActiveOrder>> GetReadyPickupsAsync(int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<WaiterActiveOrder>>([]);
}
