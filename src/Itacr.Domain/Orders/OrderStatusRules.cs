namespace Itacr.Domain.Orders;

/// <summary>An order moves one step at a time: New, InPreparation, Ready, Delivered. Delivered is final and nothing goes back.</summary>
public static class OrderStatusRules
{
    /// <summary>The only status an order in <paramref name="status"/> can move to, or null when it is final.</summary>
    public static OrderStatus? NextOf(OrderStatus status) => status switch
    {
        OrderStatus.New => OrderStatus.InPreparation,
        OrderStatus.InPreparation => OrderStatus.Ready,
        OrderStatus.Ready => OrderStatus.Delivered,
        _ => null,
    };

    public static bool CanChange(OrderStatus from, OrderStatus to) => NextOf(from) == to;
}
