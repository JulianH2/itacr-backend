using Itacr.Api.Common;

namespace Itacr.Api.Features.Orders;

public sealed class OrderNote : AuditableEntity
{
    public Guid OrderId { get; set; }

    public required string Text { get; set; }
}
