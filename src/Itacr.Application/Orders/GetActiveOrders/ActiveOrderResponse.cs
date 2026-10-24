using Itacr.Domain.Orders;

namespace Itacr.Application.Orders.GetActiveOrders;

/// <summary>An order of the waiter that is still active, to show on the dashboard and open its detail.</summary>
/// <param name="Id">Order identifier.</param>
/// <param name="OrderNumber">Number shown to staff and customers.</param>
/// <param name="Channel">DineIn or TakeAway.</param>
/// <param name="Status">New, InPreparation, or Ready. A delivered order is not active.</param>
/// <param name="TableNumber">Table of a dine-in order, null otherwise.</param>
/// <param name="CreatedAt">UTC instant when the order was created.</param>
/// <param name="LastStatusChangeAt">UTC instant of its last status change, to tell how long it has been ready.</param>
public sealed record ActiveOrderResponse(
    Guid Id,
    int OrderNumber,
    OrderChannel Channel,
    OrderStatus Status,
    int? TableNumber,
    DateTime CreatedAt,
    DateTime LastStatusChangeAt);
