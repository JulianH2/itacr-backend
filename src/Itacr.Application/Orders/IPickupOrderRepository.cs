using Itacr.Domain.Customers;
using Itacr.Domain.Orders;

namespace Itacr.Application.Orders;

public sealed record PickupOrderItem(string Name, int Quantity, decimal UnitPrice, decimal Subtotal);

/// <summary>A pickup order as the customer may see it: never the customer, the table, the waiter, or an internal id.</summary>
public sealed record PickupOrder(
    Guid ClientOrderId,
    int OrderNumber,
    OrderStatus Status,
    decimal Total,
    DateTime CreatedAt,
    IReadOnlyList<PickupOrderItem> Items);

public enum PickupCreateOutcome
{
    Created,

    /// <summary>An order with the same <see cref="Order.ClientOrderId"/> was already saved.</summary>
    AlreadyExists,

    /// <summary>The phone already has the most active orders it may have.</summary>
    LimitReached,
}

public interface IPickupOrderRepository
{
    /// <summary>The pickup order with this id, or null when there is none or it is not a MobileApp order.</summary>
    Task<PickupOrder?> GetByClientOrderIdAsync(Guid clientOrderId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the order, its items, and its history in one transaction after finding the customer by phone (or creating it and
    /// keeping the name just typed) and checking that the phone has fewer than <paramref name="maxActiveOrders"/> active orders.
    /// Assigns the keys and <see cref="Order.OrderNumber"/> only when it returns <see cref="PickupCreateOutcome.Created"/>;
    /// otherwise nothing is saved.
    /// </summary>
    Task<PickupCreateOutcome> CreateAsync(Order order, Customer customer, int maxActiveOrders, CancellationToken cancellationToken);
}
