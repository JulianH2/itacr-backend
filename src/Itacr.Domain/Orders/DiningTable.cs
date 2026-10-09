using Itacr.Domain.Common;

namespace Itacr.Domain.Orders;

public sealed class DiningTable : AuditableEntity
{
    public int Number { get; set; }
}
