using Itacr.Domain.Common;

namespace Itacr.Domain.Orders;

public sealed class Order : AuditableEntity
{
    public Guid ClientOrderId { get; set; }

    public int OrderNumber { get; set; }

    public OrderChannel Channel { get; set; }

    public OrderStatus Status { get; set; }

    public Guid? TableId { get; set; }

    public Guid? WaiterId { get; set; }

    public Guid? CustomerId { get; set; }

    public decimal Total { get; set; }

    public DateTime LastStatusChangeAt { get; set; }

    public DateTime? ReadyAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public ICollection<OrderItem> Items { get; } = [];

    public ICollection<OrderNote> Notes { get; } = [];

    public ICollection<OrderStatusHistory> StatusHistory { get; } = [];

    /// <summary>
    /// A new order: status <see cref="OrderStatus.New"/>, no items yet, and its first history row. The repositories set the
    /// keys; the database sets <see cref="OrderNumber"/>.
    /// </summary>
    public static Order Create(Guid clientOrderId, OrderChannel channel, Guid? tableId, Guid? waiterId, Guid? customerId, DateTime now)
    {
        var order = new Order
        {
            ClientOrderId = clientOrderId,
            Channel = channel,
            Status = OrderStatus.New,
            TableId = tableId,
            WaiterId = waiterId,
            CustomerId = customerId,
            LastStatusChangeAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };
        order.StatusHistory.Add(new OrderStatusHistory { FromStatus = null, ToStatus = OrderStatus.New, ChangedById = waiterId, CreatedAt = now, UpdatedAt = now });
        return order;
    }

    /// <summary>Adds an item priced at <paramref name="unitPrice"/> (copied from the product at order time) and recalculates <see cref="Total"/>.</summary>
    public OrderItem AddItem(Guid productId, int quantity, decimal unitPrice, string? size, string? drink, IEnumerable<string> extras, IEnumerable<string> exclusions, DateTime now)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quantity, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

        var item = new OrderItem
        {
            OrderId = Id,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Subtotal = unitPrice * quantity,
            Size = size,
            Drink = drink,
            Extras = [.. extras],
            Exclusions = [.. exclusions],
            CreatedAt = now,
            UpdatedAt = now,
        };
        Items.Add(item);
        Total = Items.Sum(existing => existing.Subtotal);
        UpdatedAt = now;
        return item;
    }

    /// <summary>
    /// Moves the order to <paramref name="next"/> when <see cref="OrderStatusRules"/> allows it and records the change;
    /// returns false, changing nothing, when it does not.
    /// </summary>
    public bool TryChangeStatus(OrderStatus next, Guid? changedById, DateTime now)
    {
        if (!OrderStatusRules.CanChange(Status, next))
        {
            return false;
        }

        StatusHistory.Add(new OrderStatusHistory { OrderId = Id, FromStatus = Status, ToStatus = next, ChangedById = changedById, CreatedAt = now, UpdatedAt = now });
        Status = next;
        LastStatusChangeAt = now;
        UpdatedAt = now;
        if (next == OrderStatus.Ready)
        {
            ReadyAt = now;
        }
        else if (next == OrderStatus.Delivered)
        {
            DeliveredAt = now;
        }

        return true;
    }
}
