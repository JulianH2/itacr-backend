using Itacr.Domain.Common;

namespace Itacr.Domain.Orders;

public sealed class OrderNote : AuditableEntity
{
    public Guid OrderId { get; set; }

    public required string Text { get; set; }
}
