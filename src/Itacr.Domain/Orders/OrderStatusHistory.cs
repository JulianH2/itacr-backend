using Itacr.Domain.Common;

namespace Itacr.Domain.Orders;

/// <summary>One status change of an order. <see cref="FromStatus"/> is null on the row that records the creation.</summary>
public sealed class OrderStatusHistory : AuditableEntity
{
    public Guid OrderId { get; set; }

    public OrderStatus? FromStatus { get; set; }

    public OrderStatus ToStatus { get; set; }

    /// <summary>The employee who made the change; null when the system made it (for example an order placed from the mobile app).</summary>
    public Guid? ChangedById { get; set; }
}
