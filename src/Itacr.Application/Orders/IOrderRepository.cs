using Itacr.Domain.Orders;

namespace Itacr.Application.Orders;

public sealed record PlacedOrderItem(Guid ProductId, string Name, int Quantity, decimal UnitPrice, decimal Subtotal);

/// <summary>An order as it was saved, with the table number and the product names the waiter sees.</summary>
public sealed record PlacedOrder(
    Guid Id,
    Guid ClientOrderId,
    int OrderNumber,
    OrderChannel Channel,
    OrderStatus Status,
    Guid? WaiterId,
    int? TableNumber,
    decimal Total,
    DateTime CreatedAt,
    IReadOnlyList<PlacedOrderItem> Items);

/// <summary>The last change of an order's history; <see cref="FromStatus"/> is null for the change that created it.</summary>
public sealed record OrderStatusChange(OrderStatus? FromStatus, OrderStatus ToStatus, Guid? ChangedById, DateTime ChangedAt);

/// <summary>The current status of an order and its last change, to validate the next one.</summary>
public sealed record OrderStatusState(Guid Id, OrderStatus Status, OrderStatusChange LastChange);

public sealed record OrderDetailItem(string Name, int Quantity, decimal UnitPrice, decimal Subtotal);

/// <summary><see cref="WaiterId"/> is only to decide who may read the order; it is never part of a response.</summary>
public sealed record OrderDetail(
    Guid Id,
    int OrderNumber,
    OrderChannel Channel,
    OrderStatus Status,
    int? TableNumber,
    Guid? WaiterId,
    decimal Total,
    DateTime CreatedAt,
    DateTime LastStatusChangeAt,
    DateTime? ReadyAt,
    IReadOnlyList<OrderDetailItem> Items,
    IReadOnlyList<OrderStatusChange> History);

public sealed record RecentOrder(Guid Id, int OrderNumber, OrderChannel Channel, OrderStatus Status, int? TableNumber, DateTime CreatedAt);

/// <summary>An order of a waiter that is not delivered yet.</summary>
public sealed record WaiterActiveOrder(Guid Id, int OrderNumber, OrderChannel Channel, OrderStatus Status, int? TableNumber, DateTime CreatedAt, DateTime LastStatusChangeAt);

public interface IOrderRepository
{
    Task<PlacedOrder?> GetByClientOrderIdAsync(Guid clientOrderId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the order, its items, and its history in one transaction, and assigns the keys and
    /// <see cref="Order.OrderNumber"/>. Returns false, saving nothing, when an order with the same
    /// <see cref="Order.ClientOrderId"/> already exists.
    /// </summary>
    Task<bool> CreateAsync(Order order, CancellationToken cancellationToken);

    Task<OrderStatusState?> GetStatusAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>
    /// Moves the order from <paramref name="from"/> to <paramref name="to"/> and records the change, in one transaction. Returns
    /// false, changing nothing, when the order is no longer in <paramref name="from"/> (someone else changed it first).
    /// </summary>
    Task<bool> ChangeStatusAsync(Guid orderId, OrderStatus from, OrderStatus to, Guid changedById, DateTime now, CancellationToken cancellationToken);

    Task<OrderDetail?> GetDetailAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>The latest orders of a waiter created since <paramref name="since"/>, newest first.</summary>
    Task<IReadOnlyList<RecentOrder>> GetRecentByWaiterAsync(Guid waiterId, DateTime since, int take, CancellationToken cancellationToken);

    /// <summary>The orders of a waiter in New, InPreparation, or Ready, newest first.</summary>
    Task<IReadOnlyList<WaiterActiveOrder>> GetActiveByWaiterAsync(Guid waiterId, int take, CancellationToken cancellationToken);
}
