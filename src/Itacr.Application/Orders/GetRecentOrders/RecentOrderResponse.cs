using Itacr.Domain.Orders;

namespace Itacr.Application.Orders.GetRecentOrders;

/// <summary>An order the waiter sent recently, to open its detail.</summary>
/// <param name="Id">Order identifier.</param>
/// <param name="OrderNumber">Number shown to staff and customers.</param>
/// <param name="Channel">DineIn or TakeAway.</param>
/// <param name="Status">Current status.</param>
/// <param name="TableNumber">Table of a dine-in order, null otherwise.</param>
/// <param name="CreatedAt">UTC instant when the order was created.</param>
public sealed record RecentOrderResponse(Guid Id, int OrderNumber, OrderChannel Channel, OrderStatus Status, int? TableNumber, DateTime CreatedAt);
