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

public interface IOrderRepository
{
    Task<PlacedOrder?> GetByClientOrderIdAsync(Guid clientOrderId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the order, its items, and its history in one transaction, and assigns the keys and
    /// <see cref="Order.OrderNumber"/>. Returns false, saving nothing, when an order with the same
    /// <see cref="Order.ClientOrderId"/> already exists.
    /// </summary>
    Task<bool> CreateAsync(Order order, CancellationToken cancellationToken);
}
