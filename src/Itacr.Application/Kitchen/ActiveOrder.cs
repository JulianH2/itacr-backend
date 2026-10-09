using Itacr.Domain.Orders;

namespace Itacr.Application.Kitchen;

/// <summary>A product of an active order, with the customization the kitchen has to follow.</summary>
public sealed record ActiveOrderItem(
    string ProductName,
    int Quantity,
    string? Size,
    string? Drink,
    IReadOnlyList<string> Extras,
    IReadOnlyList<string> Exclusions);

/// <summary>An order the kitchen has not finished with, with the table number and the product names it shows.</summary>
public sealed record ActiveOrder(
    Guid Id,
    int OrderNumber,
    OrderChannel Channel,
    OrderStatus Status,
    int? TableNumber,
    DateTime CreatedAt,
    IReadOnlyList<ActiveOrderItem> Items);
