using Itacr.Domain.Orders;

namespace Itacr.Application.Orders.GetOrderDetail;

/// <param name="Name">Name of the product as it appears on the menu.</param>
/// <param name="Quantity">Units ordered.</param>
/// <param name="UnitPrice">Price of one unit when the order was taken.</param>
/// <param name="Subtotal">Quantity times unit price.</param>
public sealed record OrderDetailItemResponse(string Name, int Quantity, decimal UnitPrice, decimal Subtotal);

/// <summary>One step of the order timeline.</summary>
/// <param name="Status">Status the order reached.</param>
/// <param name="ChangedAt">UTC instant of the change.</param>
/// <param name="Undone">True when the step went back because the kitchen undid its previous one.</param>
public sealed record OrderTimelineEntryResponse(OrderStatus Status, DateTime ChangedAt, bool Undone);

/// <summary>An order with its products and the timeline of its statuses.</summary>
/// <param name="Id">Order identifier.</param>
/// <param name="OrderNumber">Number shown to staff and customers.</param>
/// <param name="Channel">DineIn, TakeAway, or MobileApp.</param>
/// <param name="Status">Current status.</param>
/// <param name="TableNumber">Table of a dine-in order, null otherwise.</param>
/// <param name="Total">Sum of the subtotals.</param>
/// <param name="CreatedAt">UTC instant when the order was created.</param>
/// <param name="ReadyAt">UTC instant when it was last marked Ready, or null.</param>
/// <param name="DeliveredAt">UTC instant when it was delivered, or null.</param>
/// <param name="Items">The products of the order, in the order they were added.</param>
/// <param name="Timeline">Every status change, oldest first, starting with New.</param>
public sealed record OrderDetailResponse(
    Guid Id,
    int OrderNumber,
    OrderChannel Channel,
    OrderStatus Status,
    int? TableNumber,
    decimal Total,
    DateTime CreatedAt,
    DateTime? ReadyAt,
    DateTime? DeliveredAt,
    IReadOnlyList<OrderDetailItemResponse> Items,
    IReadOnlyList<OrderTimelineEntryResponse> Timeline);
