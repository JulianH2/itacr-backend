using Itacr.Application.Orders.CreateOrder;

namespace Itacr.Application.Orders.UpdateOrder;

/// <summary>
/// The new content of an order that is still New or InPreparation: it replaces the products, and sets the table of a
/// dine-in order (a take-away order has none). <paramref name="Note"/> is added to the order's notes; the earlier notes stay.
/// Prices are never part of the request, they come from the menu.
/// </summary>
public sealed record UpdateOrderCommand(Guid? TableId, IReadOnlyList<CreateOrderItem> Items, string? Note = null);
