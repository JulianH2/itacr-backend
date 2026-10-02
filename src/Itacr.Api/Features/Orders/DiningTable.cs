using Itacr.Api.Common;

namespace Itacr.Api.Features.Orders;

public sealed class DiningTable : AuditableEntity
{
    public int Number { get; set; }
}
