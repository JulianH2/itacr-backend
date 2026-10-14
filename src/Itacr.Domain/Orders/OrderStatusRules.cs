namespace Itacr.Domain.Orders;

/// <summary>
/// An order moves one step at a time: New, InPreparation, Ready, Delivered. Delivered is final. The only way back is undoing
/// the last kitchen step within <see cref="UndoWindow"/>, for an accidental tap on the kitchen board.
/// </summary>
public static class OrderStatusRules
{
    /// <summary>How long after a kitchen step it can still be undone. The board offers it for less, to allow for the network.</summary>
    public static readonly TimeSpan UndoWindow = TimeSpan.FromSeconds(15);

    /// <summary>The statuses the kitchen sets; Delivered belongs to the waiter.</summary>
    public static bool IsKitchenStep(OrderStatus status) => status is OrderStatus.InPreparation or OrderStatus.Ready;

    /// <summary>The only status an order in <paramref name="status"/> can move to, or null when it is final.</summary>
    public static OrderStatus? NextOf(OrderStatus status) => status switch
    {
        OrderStatus.New => OrderStatus.InPreparation,
        OrderStatus.InPreparation => OrderStatus.Ready,
        OrderStatus.Ready => OrderStatus.Delivered,
        _ => null,
    };

    public static bool CanChange(OrderStatus from, OrderStatus to) => NextOf(from) == to;

    /// <summary>
    /// True when the step <paramref name="from"/> to <paramref name="to"/>, taken at <paramref name="changedAt"/>, is a kitchen step
    /// that can still be undone at <paramref name="now"/>.
    /// </summary>
    public static bool CanUndo(OrderStatus? from, OrderStatus to, DateTime changedAt, DateTime now) =>
        from is { } previous && IsKitchenStep(to) && CanChange(previous, to) && now - changedAt <= UndoWindow;
}
