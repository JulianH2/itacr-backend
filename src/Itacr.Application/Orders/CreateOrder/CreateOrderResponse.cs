using Itacr.Domain.Orders;

namespace Itacr.Application.Orders.CreateOrder;

public sealed record CreateOrderItemResponse(Guid ProductId, string Name, int Quantity, decimal UnitPrice, decimal Subtotal);

/// <summary>What the waiter sees after sending the order: its number, where it goes, and its total.</summary>
public sealed record CreateOrderResponse(
    Guid Id,
    Guid ClientOrderId,
    int OrderNumber,
    OrderStatus Status,
    OrderChannel Channel,
    int? TableNumber,
    decimal Total,
    DateTime CreatedAt,
    IReadOnlyList<CreateOrderItemResponse> Items);

/// <summary><paramref name="Created"/> is false when the request repeated an order that already existed.</summary>
public sealed record CreateOrderOutcome(CreateOrderResponse Order, bool Created);
