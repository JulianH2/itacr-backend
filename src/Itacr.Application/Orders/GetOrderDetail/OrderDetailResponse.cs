using Itacr.Domain.Orders;

namespace Itacr.Application.Orders.GetOrderDetail;

/// <param name="Name">Name of the product as it appears on the menu.</param>
/// <param name="Quantity">Units ordered.</param>
/// <param name="UnitPrice">Price of one unit when the order was taken.</param>
/// <param name="Subtotal">Quantity times unit price.</param>
/// <param name="Size">Chosen size of a combo, or null.</param>
/// <param name="Drink">Chosen drink of a combo, or null.</param>
/// <param name="Extras">Extras added to a combo; empty when there are none.</param>
/// <param name="Exclusions">Ingredients left out of a combo; empty when there are none.</param>
/// <param name="ProductId">The menu product, to modify the order.</param>
/// <param name="SizeId">Id of the chosen size, or null when there is none or it is no longer on the menu.</param>
/// <param name="DrinkId">Id of the chosen drink, or null when there is none or it is no longer on the menu.</param>
/// <param name="ExtraIds">Ids of the extras in the order of <paramref name="Extras"/>; an entry is null when that extra is no longer on the menu.</param>
/// <param name="ExclusionIds">Ids of the exclusions in the order of <paramref name="Exclusions"/>; an entry is null when it is no longer on the menu.</param>
public sealed record OrderDetailItemResponse(
    string Name,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    string? Size,
    string? Drink,
    IReadOnlyList<string> Extras,
    IReadOnlyList<string> Exclusions,
    Guid ProductId,
    Guid? SizeId,
    Guid? DrinkId,
    IReadOnlyList<Guid?> ExtraIds,
    IReadOnlyList<Guid?> ExclusionIds);

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
/// <param name="ModifiedAt">UTC instant when the waiter last modified the order, or null when it never was.</param>
/// <param name="Items">The products of the order, in the order they were added.</param>
/// <param name="Notes">The notes the waiter added when modifying the order, oldest first.</param>
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
    DateTime? ModifiedAt,
    IReadOnlyList<OrderDetailItemResponse> Items,
    IReadOnlyList<string> Notes,
    IReadOnlyList<OrderTimelineEntryResponse> Timeline)
{
    internal static OrderDetailResponse From(OrderDetail order) => new(
        order.Id,
        order.OrderNumber,
        order.Channel,
        order.Status,
        order.TableNumber,
        order.Total,
        order.CreatedAt,
        order.ReadyAt,
        order.DeliveredAt,
        order.ModifiedAt,
        [.. order.Items.Select(item => new OrderDetailItemResponse(item.Name, item.Quantity, item.UnitPrice, item.Subtotal, item.Size, item.Drink, item.Extras, item.Exclusions, item.ProductId, item.SizeId, item.DrinkId, item.ExtraIds, item.ExclusionIds))],
        order.Notes,
        [.. order.History.Select(change => new OrderTimelineEntryResponse(change.ToStatus, change.ChangedAt, change.FromStatus > change.ToStatus))]);
}
