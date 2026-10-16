using Itacr.Domain.Orders;

namespace Itacr.Application.Orders;

/// <summary>A product of the pickup order.</summary>
/// <param name="Name">Name of the product as it appears on the menu.</param>
/// <param name="Quantity">Units ordered.</param>
/// <param name="UnitPrice">Menu price of one unit in MXN.</param>
/// <param name="Subtotal">Unit price times quantity.</param>
public sealed record PickupOrderItemResponse(string Name, int Quantity, decimal UnitPrice, decimal Subtotal);

/// <summary>
/// The pickup order of a customer, shown right after sending it and while following it. It has the order number to say at the
/// counter, never the name or the phone of the customer and never an internal id: the customer follows the order with the
/// <c>clientOrderId</c> their phone made.
/// </summary>
/// <param name="ClientOrderId">The id the customer's device generated; it is the receipt.</param>
/// <param name="OrderNumber">Number said at the counter when the customer comes to pick it up.</param>
/// <param name="Status">New, InPreparation, Ready, or Delivered.</param>
/// <param name="Total">Total to pay at pickup, in MXN.</param>
/// <param name="CreatedAt">UTC instant when the order was created.</param>
/// <param name="Items">The products of the order.</param>
public sealed record PickupOrderResponse(
    Guid ClientOrderId,
    int OrderNumber,
    OrderStatus Status,
    decimal Total,
    DateTime CreatedAt,
    IReadOnlyList<PickupOrderItemResponse> Items)
{
    public static PickupOrderResponse From(PickupOrder order) => new(
        order.ClientOrderId,
        order.OrderNumber,
        order.Status,
        order.Total,
        order.CreatedAt,
        [.. order.Items.Select(item => new PickupOrderItemResponse(item.Name, item.Quantity, item.UnitPrice, item.Subtotal))]);
}
