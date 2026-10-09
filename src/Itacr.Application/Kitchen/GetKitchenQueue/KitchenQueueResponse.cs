using Itacr.Domain.Orders;

namespace Itacr.Application.Kitchen.GetKitchenQueue;

/// <summary>A product of an order with its customization.</summary>
/// <param name="ProductName">Name of the product as it appears on the menu.</param>
/// <param name="Quantity">Units ordered.</param>
/// <param name="Size">Chosen size, or null.</param>
/// <param name="Drink">Chosen drink, or null.</param>
/// <param name="Extras">Extras to add; empty when there are none.</param>
/// <param name="Exclusions">Ingredients to leave out; empty when there are none.</param>
public sealed record KitchenQueueItemResponse(
    string ProductName,
    int Quantity,
    string? Size,
    string? Drink,
    IReadOnlyList<string> Extras,
    IReadOnlyList<string> Exclusions);

/// <summary>An order on the kitchen board.</summary>
/// <param name="Id">Order identifier.</param>
/// <param name="OrderNumber">Number shown to staff and customers.</param>
/// <param name="Channel">Where it comes from: DineIn or TakeAway (waiter), MobileApp (customer pickup).</param>
/// <param name="Status">New, InPreparation, or Ready; it decides the board column.</param>
/// <param name="TableNumber">Table of a dine-in order, null otherwise.</param>
/// <param name="CreatedAt">UTC instant when the order was created; the queue is ordered by it.</param>
/// <param name="Items">The products of the order.</param>
public sealed record KitchenQueueOrderResponse(
    Guid Id,
    int OrderNumber,
    OrderChannel Channel,
    OrderStatus Status,
    int? TableNumber,
    DateTime CreatedAt,
    IReadOnlyList<KitchenQueueItemResponse> Items);

/// <summary>The active orders in one queue, oldest first.</summary>
/// <param name="ServerTime">UTC instant of the server when it answered, so timers do not depend on the device clock.</param>
/// <param name="Orders">Orders in New, InPreparation, or Ready, ordered by <c>CreatedAt</c> whatever their channel.</param>
public sealed record KitchenQueueResponse(DateTime ServerTime, IReadOnlyList<KitchenQueueOrderResponse> Orders);
