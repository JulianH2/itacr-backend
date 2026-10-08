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
}
